namespace HiveCore.Utilities;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    // ============================================================
    // App info
    // ============================================================
    public const string AppName = "HiveCore";
    public const string AppVersion = "1.0.0";
    public const string DefaultCulture = "en-US";

    // ============================================================
    // Authentication / Authorization
    // ============================================================
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string User = "User";
        public const string Verified = "Verified";
        public const string Suspended = "Suspended";
    }

    public static class Policies
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireModerator = "RequireModerator";
        public const string RequireVerified = "RequireVerified";
        public const string RequireActiveUser = "RequireActiveUser";
    }

    public static class Claims
    {
        public const string UserId = "uid";
        public const string Username = "uname";
        public const string IsVerified = "is_verified";
        public const string IsAdmin = "is_admin";
    }

    // ============================================================
    // Cookies / Headers
    // ============================================================
    public static class Cookies
    {
        public const string Auth = "HiveCore.Auth";
        public const string Antiforgery = "HiveCore.Antiforgery";
        public const string Session = "HiveCore.Session";
        public const string Theme = "HiveCore.Theme";
    }

    public static class Headers
    {
        public const string ApiKey = "X-Api-Key";
        public const string ClientId = "X-ClientId";
        public const string RealIp = "X-Real-IP";
        public const string CsrfToken = "X-CSRF-TOKEN";
        public const string RequestId = "X-Request-Id";
    }

    // ============================================================
    // Limits
    // ============================================================
    public static class Limits
    {
        public const int MaxBioLength = 500;
        public const int MaxDisplayNameLength = 80;
        public const int MaxPostLength = 2000;
        public const int MaxCommentLength = 1000;
        public const int MaxHashtagsPerPost = 10;
        public const int MaxMentionsPerPost = 20;
        public const int MaxMediaPerPost = 4;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int FeedPageSize = 25;
        public const int CommentsPageSize = 30;
    }

    public static class Media
    {
        public const long MaxAvatarSizeBytes = 5 * 1024 * 1024;      // 5 MB
        public const long MaxCoverSizeBytes = 10 * 1024 * 1024;      // 10 MB
        public const long MaxPostImageSizeBytes = 15 * 1024 * 1024;  // 15 MB
        public const long MaxPostVideoSizeBytes = 100 * 1024 * 1024; // 100 MB

        public static readonly string[] AllowedImageExtensions =
            { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public static readonly string[] AllowedVideoExtensions =
            { ".mp4", ".webm", ".mov" };

        public static readonly string[] AllowedImageContentTypes =
            { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public static readonly string[] AllowedVideoContentTypes =
            { "video/mp4", "video/webm", "video/quicktime" };
    }

    // ============================================================
    // Cache keys
    // ============================================================
    public static class CacheKeys
    {
        public const string TrendingPosts = "trending:posts";
        public const string TrendingHashtags = "trending:hashtags";
        public const string SuggestedUsers = "suggested:users";
        public const string UserProfileFormat = "user:profile:{0}";
        public const string UserFeedFormat = "user:feed:{0}:p{1}";
        public const string PostDetailsFormat = "post:details:{0}";
    }

    // ============================================================
    // Regex patterns
    // ============================================================
    public static class Patterns
    {
        public const string Username = @"^[a-zA-Z0-9_.]{3,30}$";
        public const string Hashtag = @"#[a-zA-Z0-9_]{1,50}";
        public const string Mention = @"@[a-zA-Z0-9_.]{1,30}";
        public const string Url = @"https?://[^\s]+";
        public const string StrongPassword =
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,100}$";
    }

    // ============================================================
    // Route names
    // ============================================================
    public static class Routes
    {
        public const string Home = "/";
        public const string Login = "/Account/Login";
        public const string Register = "/Account/Register";
        public const string Logout = "/Account/Logout";
        public const string Profile = "/Profile/{0}";
        public const string Post = "/Post/{0}";
        public const string Explore = "/Explore";
        public const string Notifications = "/Notifications";
        public const string Bookmarks = "/Bookmarks";
        public const string Settings = "/Profile/Settings";
    }

    // ============================================================
    // Upload paths
    // ============================================================
    public static class UploadPaths
    {
        public const string Root = "wwwroot/uploads";
        public const string Avatars = "avatars";
        public const string Covers = "covers";
        public const string Posts = "posts";
        public const string Music = "music";
    }
}