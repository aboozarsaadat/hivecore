using System.Reflection;
using FluentValidation;
using HiveCore.Repositories.Implementations;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Implementations;
using HiveCore.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HiveCore.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register HiveCore services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all repositories used by HiveCore.
    /// </summary>
    public static IServiceCollection AddHiveCoreRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        return services;
    }

    /// <summary>
    /// Registers all application services used by HiveCore.
    /// </summary>
    public static IServiceCollection AddHiveCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMediaService, MediaService>();
        return services;
    }

    /// <summary>
    /// Registers all FluentValidation validators in the given assembly.
    /// </summary>
    public static IServiceCollection AddHiveCoreValidators(
        this IServiceCollection services,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        services.AddValidatorsFromAssembly(assembly);
        return services;
    }

    /// <summary>
    /// Registers AutoMapper profiles in the given assembly.
    /// </summary>
    public static IServiceCollection AddHiveCoreMapping(
        this IServiceCollection services,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        services.AddAutoMapper(assembly);
        return services;
    }
}