using Ganss.Xss;

namespace final_p_01.Services;

public interface IRichTextSanitizer
{
    string Sanitize(string? html);
}

/// <summary>
/// Allowlist-based sanitizer for TinyMCE-authored rich text (posts, pillar pages,
/// city pages, project descriptions). Strips script/style payloads, event handlers
/// and javascript: URLs while keeping editorial markup (headings, tables, figures,
/// images, links, media iframes) intact.
/// </summary>
public class RichTextSanitizer : IRichTextSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public RichTextSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedAttributes.Add("style");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("colspan");
        _sanitizer.AllowedAttributes.Add("rowspan");
        _sanitizer.AllowedAttributes.Add("loading");
        _sanitizer.AllowedAttributes.Add("fetchpriority");
        _sanitizer.AllowedAttributes.Add("allow");
        _sanitizer.AllowedAttributes.Add("allowfullscreen");
        _sanitizer.AllowedAttributes.Add("frameborder");
        _sanitizer.AllowedAttributes.Add("dir");
        _sanitizer.AllowedTags.Add("figure");
        _sanitizer.AllowedTags.Add("figcaption");
        _sanitizer.AllowedTags.Add("picture");
        _sanitizer.AllowedTags.Add("source");
        _sanitizer.AllowedTags.Add("iframe");
        _sanitizer.AllowedCssProperties.Add("aspect-ratio");
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html ?? string.Empty;
        return _sanitizer.Sanitize(html);
    }
}
