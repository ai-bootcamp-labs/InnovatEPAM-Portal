namespace InnovatEpam.Portal.Application.Storage;

/// <summary>
/// Storage abstraction for opaque attachment payloads (R3, FR-010..012).
/// Phase 1 implementation persists to the local filesystem; production phases
/// can swap in object storage without touching the application layer.
/// </summary>
public interface IAttachmentStorage
{
    /// <summary>Persists the stream and returns the opaque storage key.</summary>
    Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken);

    /// <summary>Opens a read stream for the previously stored payload.</summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>Removes the payload. No-ops if the key is unknown.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
