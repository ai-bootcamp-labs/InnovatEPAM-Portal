using System.Security.Cryptography;
using InnovatEpam.Portal.Application.Storage;
using Microsoft.Extensions.Options;

namespace InnovatEpam.Portal.Infrastructure.Storage;

/// <summary>Bound from configuration section <c>Attachments:</c>.</summary>
public sealed class AttachmentStorageOptions
{
    public string RootPath { get; set; } = "./.attachments";
}

/// <summary>
/// Filesystem-backed <see cref="IAttachmentStorage"/> (T032). Storage layout:
/// <c>{RootPath}/{yyyy}/{MM}/{guid}{ext}</c>. The implementation never trusts
/// the original filename — only the validated extension is preserved.
/// </summary>
public sealed class FileSystemAttachmentStorage : IAttachmentStorage
{
    private readonly string _rootPath;

    public FileSystemAttachmentStorage(IOptions<AttachmentStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        // Materialize the root at startup so a misconfigured working directory
        // surfaces immediately rather than on the first upload.
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var safeExt = SanitizeExtension(fileExtension);
        var now = DateTimeOffset.UtcNow;
        var relative = Path.Combine(
            now.Year.ToString("0000"),
            now.Month.ToString("00"),
            $"{Guid.NewGuid():N}{safeExt}");
        var absolute = Path.Combine(_rootPath, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using var output = new FileStream(
            absolute, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);

        return relative.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = ResolveAbsolute(storageKey);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = ResolveAbsolute(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string ResolveAbsolute(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        var combined = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!combined.StartsWith(_rootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage key resolves outside the root path.");
        }
        return combined;
    }

    private static string SanitizeExtension(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            return string.Empty;
        }
        var trimmed = ext.Trim();
        if (!trimmed.StartsWith('.'))
        {
            trimmed = "." + trimmed;
        }
        foreach (var ch in trimmed)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '.'))
            {
                throw new ArgumentException("Invalid file extension.", nameof(ext));
            }
        }
        return trimmed.ToLowerInvariant();
    }

    /// <summary>Streaming SHA-256 helper; not part of the interface but used by callers.</summary>
    public static async Task<byte[]> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        return await sha.ComputeHashAsync(stream, cancellationToken);
    }
}
