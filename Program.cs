using HiveCore.Data;
using HiveCore.Extensions;
using HiveCore.Filters;
using HiveCore.Hubs;
using HiveCore.Middleware;
using HiveCore.Models.Entities;
using HiveCore.Services.Helpers;
using HiveCore.Services.Implementations;
using HiveCore.Services.Interfaces;
using HiveCore.Repositories.Implementations;
using HiveCore.Repositories.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.IO.Compression;
using System.Reflection;
using System.Threading.RateLimiting;

// ============================================================
// Bootstrap logger (used until the host is built)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/hivecore-bootstrap-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting HiveCore...");

    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // Serilog
    // ============================================================
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "HiveCore"));

    var configuration = builder.Configuration;
    var env = builder.Environment;

    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured.");

    // ============================================================
    // Database — EF Core
    // ============================================================
    builder.Services.AddDbContext<HiveCoreDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(HiveCoreDbContext).Assembly.FullName);
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sql.CommandTimeout(60);
        });

        if (env.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // ============================================================
    // ASP.NET Core Identity
    // ============================================================
    builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 4;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";

        options.SignIn.RequireConfirmedEmail = configuration.GetValue("Identity:RequireConfirmedEmail", true);
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<HiveCoreDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = env.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "HiveCore.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ReturnUrlParameter = "returnUrl";
    });

    // ============================================================
    // MVC + Razor
    // ============================================================
    builder.Services.AddControllersWithViews(options =>
    {
        options.MaxModelBindingCollectionSize = 10_000;
        options.Filters.Add<ValidationFilter>();
    })
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/Views/{1}/Partial/{0}.cshtml");
    })
    .AddFluentValidation(fv =>
    {
        // ============================================================
        // FluentValidation — new API (v11+)
        // ============================================================
        // NOTE: The old `RegisterValidatorsFromAssembly` and
        // `ImplicitlyValidateChildProperties` are deprecated.
        // Validators are now registered via `AddValidatorsFromAssembly`
        // (below) and child validation is handled via `SetValidator`.
        fv.DisableDataAnnotationsValidation = false;
    });

    builder.Services.AddRazorPages();

    // ============================================================
    // FluentValidation — auto-validation + client-side adapters
    // ============================================================
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    // ============================================================
    // FluentValidation — register all validators from assembly
    // ============================================================
    builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

    // ============================================================
    // AutoMapper
    // ============================================================
    builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

    // ============================================================
    // HttpContext + Caching + Session
    // ============================================================
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMemoryCache();

    var redisConnection = configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "HiveCore:";
        });
    }

    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "HiveCore.Session";
        options.Cookie.SecurePolicy = env.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

    // ============================================================
    // SignalR + Real-time
    // ============================================================
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = env.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        options.MaximumReceiveMessageSize = 512 * 1024; // 512 KB
    });

    builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, SignalRUserIdProvider>();
    builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
    builder.Services.AddScoped<IRealTimeNotifier, RealTimeNotifier>();

    // ============================================================
    // Response compression
    // ============================================================
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "application/json",
            "application/xml",
            "text/plain"
        });
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
        o.Level = CompressionLevel.Optimal);
    builder.Services.Configure<GzipCompressionProviderOptions>(o =>
        o.Level = CompressionLevel.Optimal);

    // ============================================================
    // Health Checks
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddSqlServer(connectionString, name: "sqlserver", tags: new[] { "db", "sql" });

    // ============================================================
    // Swagger
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "HiveCore API",
            Version = "v1",
            Description = "HiveCore — social microblogging platform API",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "HiveCore Team",
                Email = "dev@hivecore.app"
            }
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter JWT token: Bearer {token}"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ============================================================
    // Rate limiting (built-in .NET 8)
    // ============================================================
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ip,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });

        options.AddPolicy("auth", context =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ip,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });
    });

    // ============================================================
    // CORS (named policy)
    // ============================================================
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("HiveCoreCors", policy =>
        {
            if (env.IsDevelopment())
            {
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.WithOrigins(
                        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                        ?? new[] { "https://hivecore.app" })
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        });
    });

    // ============================================================
    // Antiforgery + HSTS
    // ============================================================
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "RequestVerificationToken";
        options.Cookie.Name = "HiveCore.Antiforgery";
        options.Cookie.SecurePolicy = env.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });

    // ============================================================
    // Forwarded headers (behind reverse proxy)
    // ============================================================
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ============================================================
    // Dependency Injection — Repositories
    // ============================================================
    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    builder.Services.AddScoped<IPostRepository, PostRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IFollowRepository, FollowRepository>();

    // ============================================================
    // Dependency Injection — Services
    // ============================================================
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IPostService, PostService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IFeedService, FeedService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IMediaService, MediaService>();

    // Helpers
    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
    builder.Services.AddScoped<IJwtHelper, JwtHelper>();
    builder.Services.AddSingleton<ISlugGenerator, SlugGenerator>();

    // ============================================================
    // Build
    // ============================================================
    var app = builder.Build();

    // ============================================================
    // Middleware pipeline
    // ============================================================

    app.UseForwardedHeaders();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (!env.IsDevelopment())
    {
        app.UseHsts();
    }
    app.UseHttpsRedirection();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseResponseCompression();

    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            const int week = 60 * 60 * 24 * 7;
            ctx.Context.Response.Headers.Append(
                "Cache-Control", $"public,max-age={week}");
        }
    });

    app.UseRouting();
    app.UseCors("HiveCoreCors");
    app.UseRateLimiter();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<RateLimitingMiddleware>();

    // ============================================================
    // Swagger (Development only)
    // ============================================================
    if (env.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "HiveCore API v1");
            options.RoutePrefix = "api/docs";
            options.DocumentTitle = "HiveCore API Documentation";
        });
    }

    // ============================================================
    // Health checks
    // ============================================================
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });

    // ============================================================
    // Routes
    // ============================================================
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();

    // ============================================================
    // SignalR hubs
    // ============================================================
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHub<FeedHub>("/hubs/feed");
    app.MapHub<ChatHub>("/hubs/chat");

    // ============================================================
    // Migrate + Seed
    // ============================================================
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<HiveCoreDbContext>();
            await dbContext.Database.MigrateAsync();

            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var seederLogger = loggerFactory.CreateLogger("DataSeeder");

            await DataSeeder.SeedAsync(dbContext, userManager, roleManager, seederLogger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Database migration or seeding failed.");
            throw;
        }
    }

    Log.Information("HiveCore started successfully.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HiveCore terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposed for integration tests via WebApplicationFactory&lt;Program&gt;.
/// </summary>
public partial class Program { }