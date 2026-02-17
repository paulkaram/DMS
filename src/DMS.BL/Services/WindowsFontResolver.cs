using PdfSharp.Fonts;

namespace DMS.BL.Services;

/// <summary>
/// Custom font resolver for PDFsharp 6 on .NET 10.
/// Reads .ttf/.otf font files from the Windows Fonts directory.
/// </summary>
public class WindowsFontResolver : IFontResolver
{
    private static readonly string FontsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts));

    // Map family names to font file names
    private static readonly Dictionary<string, string> FontFileMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arial"] = "arial.ttf",
        ["Arial Bold"] = "arialbd.ttf",
        ["Arial Italic"] = "ariali.ttf",
        ["Arial Bold Italic"] = "arialbi.ttf",
        ["Times New Roman"] = "times.ttf",
        ["Times New Roman Bold"] = "timesbd.ttf",
        ["Verdana"] = "verdana.ttf",
        ["Verdana Bold"] = "verdanab.ttf",
        ["Calibri"] = "calibri.ttf",
        ["Calibri Bold"] = "calibrib.ttf",
        ["Segoe UI"] = "segoeui.ttf",
        ["Segoe UI Bold"] = "segoeuib.ttf",
    };

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var key = familyName;
        if (isBold && isItalic)
            key = $"{familyName} Bold Italic";
        else if (isBold)
            key = $"{familyName} Bold";
        else if (isItalic)
            key = $"{familyName} Italic";

        // Try exact match first, then base family
        if (FontFileMap.ContainsKey(key))
            return new FontResolverInfo(key);
        if (FontFileMap.ContainsKey(familyName))
            return new FontResolverInfo(familyName);

        // Fallback to Arial
        return new FontResolverInfo("Arial");
    }

    public byte[]? GetFont(string faceName)
    {
        if (FontFileMap.TryGetValue(faceName, out var fileName))
        {
            var path = Path.Combine(FontsDir, fileName);
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }

        // Final fallback: try to find the file name directly
        var directPath = Path.Combine(FontsDir, faceName + ".ttf");
        if (File.Exists(directPath))
            return File.ReadAllBytes(directPath);

        // Last resort: return Arial
        var arialPath = Path.Combine(FontsDir, "arial.ttf");
        if (File.Exists(arialPath))
            return File.ReadAllBytes(arialPath);

        return null;
    }
}
