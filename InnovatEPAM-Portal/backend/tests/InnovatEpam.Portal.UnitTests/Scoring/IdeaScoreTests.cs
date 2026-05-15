using FluentAssertions;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Scoring;

namespace InnovatEpam.Portal.UnitTests.Scoring;

/// <summary>
/// Phase 7 — domain-level invariants for <see cref="IdeaScore"/> (FR-006 / FR-008).
/// Domain validation must reject out-of-range ratings even if validators upstream
/// are bypassed (DB CHECK constraints are the final defence).
/// </summary>
public sealed class IdeaScoreTests
{
    private static IdeaScore Make() =>
        IdeaScore.Create(Guid.NewGuid(), Guid.NewGuid(), 3, 3, 3, 3, null);

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Create_throws_when_dimension_is_out_of_range(int bad)
    {
        var act = () => IdeaScore.Create(Guid.NewGuid(), Guid.NewGuid(), bad, 3, 3, 3, null);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_accepts_full_inclusive_range()
    {
        for (var rating = IdeaScore.MinRating; rating <= IdeaScore.MaxRating; rating++)
        {
            var ok = () => IdeaScore.Create(Guid.NewGuid(), Guid.NewGuid(), rating, rating, rating, rating, null);
            ok.Should().NotThrow();
        }
    }

    [Fact]
    public void Update_throws_when_comment_exceeds_max_length()
    {
        var score = Make();
        var comment = new string('x', IdeaScore.CommentMaxLength + 1);
        var act = () => score.Update(3, 3, 3, 3, comment);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Update_mutates_dimensions_and_touches_updated_at()
    {
        var score = Make();
        var before = score.UpdatedAt;
        Thread.Sleep(2); // ensure clock advances on fast Windows timers.
        score.Update(5, 4, 3, 2, "ok");

        score.Impact.Should().Be(5);
        score.Feasibility.Should().Be(4);
        score.Innovation.Should().Be(3);
        score.Alignment.Should().Be(2);
        score.Comment.Should().Be("ok");
        score.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
