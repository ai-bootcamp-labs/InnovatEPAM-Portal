using System.Security.Cryptography;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Application.Storage;
using InnovatEpam.Portal.Application.Validation;
using InnovatEpam.Portal.Domain.Attachments;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Application.Attachments;

/// <summary>
/// Attachment use-cases (T064): upload (create or replace), download, delete.
/// Authorisation: only the submitter or an Admin may modify attachments.
/// </summary>
public sealed class AttachmentService
{
    private readonly IPortalDbContext _db;
    private readonly IAttachmentStorage _storage;

    public AttachmentService(IPortalDbContext db, IAttachmentStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public sealed record UploadResult(AttachmentSummary Summary, bool Replaced);
    public sealed record AttachmentDownload(string OriginalFileName, string ContentType, Stream Content);

    /// <summary>
    /// Validates the file via <see cref="FileSignatureValidator"/>, persists the bytes
    /// through <see cref="IAttachmentStorage"/>, then atomically replaces any existing
    /// attachment row for the idea (FR-010, FR-011, FR-012).
    /// </summary>
    public async Task<UploadResult> UploadAsync(
        Guid ideaId,
        Stream content,
        string fileName,
        string declaredContentType,
        Guid uploaderId,
        bool uploaderIsAdmin,
        CancellationToken ct)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct)
            ?? throw new NotFoundException("Idea not found.");
        if (!uploaderIsAdmin && idea.SubmitterId != uploaderId)
        {
            throw new ForbiddenException("Only the submitter or an admin may modify attachments.");
        }

        // Buffer to memory so we can sniff signature + compute hash + persist.
        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, ct);
        if (buffered.Length == 0)
        {
            throw new DomainValidationException("File is empty.",
                new Dictionary<string, string[]> { ["File"] = ["File is empty."] });
        }
        if (buffered.Length > Attachment.MaxSizeBytes)
        {
            throw new DomainValidationException("File too large.",
                new Dictionary<string, string[]> { ["File"] = ["File exceeds the 10 MB limit."] });
        }

        buffered.Position = 0;
        var head = new byte[Math.Min(buffered.Length, 16)];
        _ = buffered.Read(head, 0, head.Length);
        var sig = FileSignatureValidator.Validate(head, declaredContentType);
        if (!sig.IsValid)
        {
            const string msg = "Unsupported file type or content does not match the declared MIME type.";
            throw new DomainValidationException(msg,
                new Dictionary<string, string[]> { ["File"] = [msg] });
        }

        buffered.Position = 0;
        var sha = await ComputeSha256Async(buffered, ct);

        buffered.Position = 0;
        var ext = ExtensionFor(declaredContentType, fileName);
        var key = await _storage.SaveAsync(buffered, ext, ct);

        // Replace-existing semantics — delete the prior row + bytes if any.
        var existing = await _db.Attachments.FirstOrDefaultAsync(a => a.IdeaId == ideaId, ct);
        var replaced = false;
        if (existing is not null)
        {
            _db.Attachments.Remove(existing);
            await _storage.DeleteAsync(existing.StorageKey, ct);
            replaced = true;
        }

        var attachment = Attachment.Create(
            ideaId,
            Path.GetFileName(fileName),
            declaredContentType,
            buffered.Length,
            key,
            sha,
            uploaderId,
            DateTimeOffset.UtcNow);
        _db.Attachments.Add(attachment);
        idea.SetAttachment(attachment.Id);
        await _db.SaveChangesAsync(ct);

        var summary = new AttachmentSummary(
            attachment.Id, attachment.OriginalFileName, attachment.ContentType,
            attachment.SizeBytes, attachment.UploadedAt);
        return new UploadResult(summary, replaced);
    }

    public async Task<AttachmentDownload> DownloadAsync(Guid ideaId, CancellationToken ct)
    {
        var att = await _db.Attachments.FirstOrDefaultAsync(a => a.IdeaId == ideaId, ct)
            ?? throw new NotFoundException("Attachment not found.");
        var stream = await _storage.OpenReadAsync(att.StorageKey, ct);
        return new AttachmentDownload(att.OriginalFileName, att.ContentType, stream);
    }

    public async Task DeleteAsync(Guid ideaId, Guid actorId, bool actorIsAdmin, CancellationToken ct)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct)
            ?? throw new NotFoundException("Idea not found.");
        if (!actorIsAdmin && idea.SubmitterId != actorId)
        {
            throw new ForbiddenException("Only the submitter or an admin may modify attachments.");
        }
        var att = await _db.Attachments.FirstOrDefaultAsync(a => a.IdeaId == ideaId, ct);
        if (att is null) return;

        _db.Attachments.Remove(att);
        idea.SetAttachment(null);
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(att.StorageKey, ct);
    }

    private static async Task<byte[]> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        return await sha.ComputeHashAsync(stream, ct);
    }

    private static string ExtensionFor(string contentType, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext)) return ext.ToLowerInvariant();
        return contentType switch
        {
            "application/pdf" => ".pdf",
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            _ => ".bin",
        };
    }

    /// <summary>Convenience overload matching <see cref="AppRole.Admin"/> by role-name.</summary>
    public Task<UploadResult> UploadAsync(
        Guid ideaId, Stream content, string fileName, string declaredContentType,
        Guid uploaderId, string roleName, CancellationToken ct) =>
        UploadAsync(ideaId, content, fileName, declaredContentType, uploaderId,
            string.Equals(roleName, AppRole.Admin, StringComparison.Ordinal), ct);
}
