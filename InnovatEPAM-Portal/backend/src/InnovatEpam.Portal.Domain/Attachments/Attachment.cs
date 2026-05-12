namespace InnovatEpam.Portal.Domain.Attachments;

/// <summary>
/// Attachment metadata. The bytes live on disk via
/// <c>IAttachmentStorage</c>. At most one attachment per idea (FR-010);
/// see data-model §5.
/// </summary>
public sealed class Attachment
{
    public const long MaxSizeBytes = 10L * 1024 * 1024;

    private Attachment() { }

    private Attachment(
        Guid id,
        Guid ideaId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        byte[] sha256,
        Guid uploadedById,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        IdeaId = ideaId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        Sha256 = sha256;
        UploadedById = uploadedById;
        UploadedAt = uploadedAt;
    }

    public Guid Id { get; private set; }
    public Guid IdeaId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public byte[] Sha256 { get; private set; } = Array.Empty<byte>();
    public Guid UploadedById { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    public static Attachment Create(
        Guid ideaId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        byte[] sha256,
        Guid uploadedById,
        DateTimeOffset uploadedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(sha256);
        if (sha256.Length != 32)
        {
            throw new ArgumentException("Sha256 must be 32 bytes.", nameof(sha256));
        }
        if (sizeBytes <= 0 || sizeBytes > MaxSizeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }
        return new Attachment(Guid.NewGuid(), ideaId, originalFileName, contentType,
            sizeBytes, storageKey, sha256, uploadedById, uploadedAt);
    }
}
