namespace InnovatEpam.Portal.Application.Validation;

/// <summary>
/// Result of a magic-byte validation pass. <see cref="IsValid"/> is true when
/// the supplied bytes match a known signature for the supplied content type.
/// </summary>
public readonly record struct FileSignatureResult(bool IsValid, string? DetectedType);

/// <summary>
/// Validates that the leading bytes of an uploaded file match its declared
/// MIME type (R4). The Phase 1 allow-list is: PDF, PNG, JPEG, DOCX, PPTX, XLSX.
/// </summary>
/// <remarks>
/// Office Open XML (DOCX/PPTX/XLSX) files are ZIP containers, so we treat any
/// payload starting with the <c>PK\x03\x04</c> signature as structurally valid
/// and rely on the declared MIME for the specific Office subtype. The unit
/// tests in T036 lock in the magic-byte tables.
/// </remarks>
public static class FileSignatureValidator
{
    public const long MaxBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    private static readonly byte[] PdfSig = "%PDF-"u8.ToArray();
    private static readonly byte[] PngSig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegSig = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] ZipSig = { 0x50, 0x4B, 0x03, 0x04 };

    public static FileSignatureResult Validate(ReadOnlySpan<byte> head, string declaredContentType)
    {
        if (!AllowedContentTypes.Contains(declaredContentType))
        {
            return new FileSignatureResult(false, null);
        }

        return declaredContentType.ToLowerInvariant() switch
        {
            "application/pdf" => new FileSignatureResult(StartsWith(head, PdfSig), "application/pdf"),
            "image/png" => new FileSignatureResult(StartsWith(head, PngSig), "image/png"),
            "image/jpeg" => new FileSignatureResult(StartsWith(head, JpegSig), "image/jpeg"),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                or "application/vnd.openxmlformats-officedocument.presentationml.presentation"
                or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                => new FileSignatureResult(StartsWith(head, ZipSig), declaredContentType),
            _ => new FileSignatureResult(false, null),
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> head, ReadOnlySpan<byte> signature)
        => head.Length >= signature.Length && head[..signature.Length].SequenceEqual(signature);
}
