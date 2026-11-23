using System.Net;
using System.Text.RegularExpressions;

namespace final_p_01.Application.Services;

public sealed record ArticleHeading(string Id, string Title, int Level);
public sealed record ArticleReadingResult(string Html, int Minutes, IReadOnlyList<ArticleHeading> Headings);

public static partial class ArticleReading
{
    public static ArticleReadingResult Prepare(string? sanitizedHtml)
    {
        var html = sanitizedHtml ?? "";
        var headings = new List<ArticleHeading>();
        var usedIds = Ids().Matches(html).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var index = 0;
        html = Headings().Replace(html, match =>
        {
            var title = WebUtility.HtmlDecode(Tags().Replace(match.Groups[3].Value, " ")).Trim();
            if (title.Length == 0) return match.Value;
            var attributes = match.Groups[2].Value;
            var existing = Ids().Match(attributes);
            var id = existing.Success ? existing.Groups[1].Value : "";
            if (id.Length == 0)
            {
                do { id = $"article-section-{++index}"; } while (!usedIds.Add(id));
                attributes += $" id=\"{id}\"";
            }
            headings.Add(new ArticleHeading(id, title, int.Parse(match.Groups[1].Value)));
            return $"<h{match.Groups[1].Value}{attributes}>{match.Groups[3].Value}</h{match.Groups[1].Value}>";
        });
        var text = WebUtility.HtmlDecode(Tags().Replace(html, " "));
        var words = Words().Matches(text).Count;
        return new ArticleReadingResult(html, Math.Max(1, (int)Math.Ceiling(words / 200d)), headings);
    }

    [GeneratedRegex("<h([23])\\b([^>]*)>(.*?)</h\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Headings();
    [GeneratedRegex("\\bid=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex Ids();
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
    [GeneratedRegex(@"\S+")]
    private static partial Regex Words();
}
