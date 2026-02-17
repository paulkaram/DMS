namespace DMS.BL.Interfaces;

public interface IWatermarkService
{
    /// <summary>
    /// Applies a text watermark to a PDF stream (NCA document protection requirement).
    /// Returns a new stream with the watermark applied, or the original stream if not a PDF.
    /// </summary>
    Task<Stream> ApplyWatermarkAsync(Stream pdfStream, string userName, string? privacyLevelName = null);

    /// <summary>
    /// Determines whether a document should be watermarked based on its privacy level.
    /// </summary>
    bool ShouldWatermark(string? privacyLevelName, int? privacyLevelValue);
}
