using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace HiveCore.Extensions;

/// <summary>
/// String helper extensions used across HiveCore.
/// Includes HTML-safe Linkify for hashtags, mentions, and URLs.
/// </summary>
public static class StringExtensions
{
    // ============================================================
    // Regex patterns (compiled for performance)
    // ============================================================
    private static readonly Regex HashtagRegex =
        new(@"(?<!\w)#([\p{L}\p{N}_]{1,50})", RegexOptions.Compiled);

    private static readonly Regex MentionRegex =
        new(@"(?<!\w)@([a-zA-Z0-9_\.]{1,30})", RegexOptions.Compiled);

    private static readonly Regex UrlRegex =
        new(@"https?://[^\s<>""]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Combined regex for linkify (single pass)
    private static readonly Regex LinkifyRegex = new(
        @"(?<url>https?://[^\s<>""]+)" +
        @"|(?<hashtag>#[\p{L}\p{N}_]{1,50})" +
        @"|(?<mention>@[a-zA-Z0-9_\.]{1,30})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NonAlphanumericRegex =
        new(@"[^a-z0-9\s-]", RegexOptions.Compiled);

    private static readonly Regex MultiWhitespaceRegex =
        new(@"\s+", RegexOptions.Compiled);

    // ============================================================
    // Basic
    // ============================================================
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value);

    public static string Truncate(this string? value, int maxLength, string suffix = "…")
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (maxLength <= 0) return string.Empty;
        if (value.Length <= maxLength) return value;
        return value[..maxLength] + suffix;
    }

    public static string ToTitleCase(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
    }

    public static string ToLowerInvariantSafe(this string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    // ============================================================
    // Slug
    // ============================================================
    public static string ToSlug(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var lower = value.ToLowerInvariant().Trim();
        var cleaned = NonAlphanumericRegex.Replace(lower, string.Empty);
        var collapsed = MultiWhitespaceRegex.Replace(cleaned, "-");
        return collapsed.Trim('-');
    }

    // ============================================================
    // Social parsing
    // ============================================================
    public static IEnumerable<string> ExtractHashtags(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (Match match in HashtagRegex.Matches(text))
            if (match.Groups.Count > 1)
                yield return match.Groups[1].Value.ToLowerInvariant();
    }

    public static IEnumerable<string> ExtractMentions(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (Match match in MentionRegex.Matches(text))
            if (match.Groups.Count > 1)
                yield return match.Groups[1].Value.ToLowerInvariant();
    }

    public static IEnumerable<string> ExtractUrls(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (Match match in UrlRegex.Matches(text))
            yield return match.Value;
    }

    // ============================================================
    // HTML encoding helpers
    // ============================================================
    /// <summary>Encodes text for safe HTML output.</summary>
    public static string HtmlEncode(this string? value)
        => string.IsNullOrEmpty(value) ? string.Empty : WebUtility.HtmlEncode(value);

    // ============================================================
    // Linkify — main entry point
    // ============================================================
    /// <summary>
    /// Converts plain-text content into HTML with clickable hashtags, mentions,
    /// and URLs. Text is HTML-encoded BEFORE linkification to prevent XSS.
    /// Safe to render with @Html.Raw(...) in Razor views.
    /// </summary>
    public static string Linkify(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1) HTML-encode the entire text first (XSS-safe baseline)
        var encoded = WebUtility.HtmlEncode(text);

        // 2) Replace with anchors. Because we already encoded, no raw '<' or '>' exist.
        var result = LinkifyRegex.Replace(encoded, match =>
        {
            if (match.Groups["url"].Success)
            {
                var url = match.Groups["url"].Value;
                // Encode & -> &amp; for attribute safety
                var attrUrl = url.Replace("&", "&amp;");
                return $"<a href=\"{attrUrl}\" target=\"_blank\" rel=\"noopener nofollow\" class=\"post-link\">{url}</a>";
            }

            if (match.Groups["hashtag"].Success)
            {
                var tag = match.Groups["hashtag"].Value; // includes leading '#'
                var slug = tag.TrimStart('#');
                return $"<a href=\"/Explore?q=%23{slug}\" class=\"post-hashtag\">{tag}</a>";
            }

            if (match.Groups["mention"].Success)
            {
                var handle = match.Groups["mention"].Value; // includes leading '@'
                var username = handle.TrimStart('@');
                return $"<a href=\"/Profile/{username}\" class=\"post-mention\">{handle}</a>";
            }

            return match.Value;
        });

        // 3) Preserve line breaks
        result = result.Replace("\r\n", "\n").Replace("\n", "<br />");

        return result;
    }

    // ============================================================
    // Masking
    // ============================================================
    public static string MaskEmail(this string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***";

        var parts = email.Split('@', 2);
        var local = parts[0];
        var domain = parts[1];

        if (local.Length <= 2)
            return $"{local[..1]}***@{domain}";

        return $"{local[..2]}***@{domain}";
    }

    public static string MaskPhone(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4)
            return "***";

        return new string('*', phone.Length - 4) + phone[^4..];
    }

    // ============================================================
    // Encoding
    // ============================================================
    public static string ToBase64(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static string FromBase64(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return string.Empty;
        }
    }
}