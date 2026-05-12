using InnovatEpam.Portal.Domain.Common;
using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Domain.Ideas;

/// <summary>
/// Innovation proposal aggregate root (FR-009..014, FR-016..021).
/// </summary>
/// <remarks>
/// Phase 2 stub — Phase 3 (T055) replaces with private setters, a
/// <c>Create(...)</c> factory, and a <c>TransitionTo(...)</c> method enforcing
/// the legal-transitions table. See data-model §4.
/// </remarks>
public class Idea : IAuditable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public IdeaStatus Status { get; set; } = IdeaStatus.Submitted;
    public Guid SubmitterId { get; set; }
    public Guid? AttachmentId { get; set; }
    public Guid? LastDecisionId { get; set; }
    public uint RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
