using FluentAssertions;
using InnovatEpam.Portal.Application.Ideas;
using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.UnitTests.Ideas;

/// <summary>
/// FR-001 / FR-003 — blind-review redaction rules (boundaries):
///   • Non-admins always see the real submitter name.
///   • Submitters always see their own identity.
///   • Admins see a real name once the idea has a terminal (Accept/Reject) decision.
///   • Otherwise admins see the alias placeholder.
/// </summary>
public sealed class BlindReviewProjectionTests
{
    [Fact]
    public void NonAdmin_caller_sees_real_identity()
    {
        IdeaService.ShouldHideIdentity(null, Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: false)
            .Should().BeFalse();
    }

    [Fact]
    public void Admin_sees_own_idea_unredacted()
    {
        var submitter = Guid.NewGuid();
        IdeaService.ShouldHideIdentity(null, submitter, submitter, callerIsAdmin: true)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(DecisionAction.Accept)]
    [InlineData(DecisionAction.Reject)]
    public void Admin_sees_real_identity_after_terminal_decision(DecisionAction action)
    {
        IdeaService.ShouldHideIdentity(action, Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true)
            .Should().BeFalse();
    }

    [Fact]
    public void Admin_sees_alias_for_unreviewed_idea()
    {
        IdeaService.ShouldHideIdentity(null, Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true)
            .Should().BeTrue();
    }

    [Fact]
    public void Admin_sees_alias_after_move_to_under_review()
    {
        IdeaService.ShouldHideIdentity(DecisionAction.MoveToUnderReview, Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true)
            .Should().BeTrue();
    }
}
