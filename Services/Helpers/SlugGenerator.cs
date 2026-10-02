using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HiveCore.Services.Helpers;

public interface ISlugGenerator
{
    string Generate(string input);
    string GenerateUnique(string input);
}

public class SlugGenerator : ISlugGenerator
{
    private static readonly Regex NonAlphanumeric =
        new(@"[^a-z0-9\s-]", RegexOptions.Compiled);

    private static readonly Regex MultiDash =
        new(@"-{2,}", RegexOptions.Compiled);

    private static readonly Regex MultiSpace =
        new(@"\s+", RegexOptions.Compiled);

    public string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize unicode
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Replace non-alphanumeric
        clean = NonAlphanumeric.Replace(clean, string.Empty);

        // Collapse whitespace → dash
        clean = MultiSpace.Replace(clean, "-");

        // Collapse multi-dash
        clean = MultiDash.Replace(clean, "-");

        // Trim leading/trailing dashes
        clean = clean.Trim('-');

        return clean;
    }

    public string GenerateUnique(string input)
    {
        var slug = Generate(input);
        if (string.IsNullOrEmpty(slug)) slug = "item";

        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{slug}-{suffix}";
    }
}