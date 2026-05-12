namespace InnovatEpam.Portal.Domain.Attachments;

/// <summary>
/// Attachment metadata. Bytes live on disk via <c>IAttachmentStorage</c>.
/// At most one attachment per idea (FR-010); see data-model §5.
/// </summary>
/// <remarks>Phase 2 stub — Phase 3 (T056) refines.</remarks>
public class Attachment
{
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public byte[] Sha256 { get; set; } = Array.Empty<byte>();
    public Guid UploadedById { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
