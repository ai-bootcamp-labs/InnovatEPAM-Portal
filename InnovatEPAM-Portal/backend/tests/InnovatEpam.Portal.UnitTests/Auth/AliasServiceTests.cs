using FluentAssertions;
using InnovatEpam.Portal.Application.Auth;
using Microsoft.Extensions.Configuration;

namespace InnovatEpam.Portal.UnitTests.Auth;

/// <summary>
/// FR-002 / FR-003 — alias service contract:
///   • Tokens are deterministic for the same (idea, reviewer) pair under a salt.
///   • Tokens diverge when the salt rotates, so a configuration change invalidates
///     historical aliases (audit / privacy safeguard).
///   • Different reviewers under the same idea get different aliases (no collision
///     leakage of reviewer identity by alias overlap).
/// </summary>
public sealed class AliasServiceTests
{
    private static AliasService BuildService(string salt = "unit-test-salt-A")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BlindReview:AliasSalt"] = salt })
            .Build();
        return new AliasService(config);
    }

    [Fact]
    public void SubmitterAlias_is_deterministic_under_same_salt()
    {
        var svc = BuildService();
        var idea = Guid.Parse("11111111-1111-1111-1111-111111111111");

        svc.SubmitterAlias(idea).Should().Be(svc.SubmitterAlias(idea));
    }

    [Fact]
    public void SubmitterAlias_changes_when_salt_rotates()
    {
        var idea = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var a = BuildService("salt-A").SubmitterAlias(idea);
        var b = BuildService("salt-B").SubmitterAlias(idea);
        a.Should().NotBe(b);
    }

    [Fact]
    public void ReviewerAlias_differs_per_reviewer_within_one_idea()
    {
        var svc = BuildService();
        var idea = Guid.NewGuid();
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();
        svc.ReviewerAlias(idea, r1).Should().NotBe(svc.ReviewerAlias(idea, r2));
    }

    [Fact]
    public void Aliases_have_expected_format()
    {
        var svc = BuildService();
        var sub = svc.SubmitterAlias(Guid.NewGuid());
        var rev = svc.ReviewerAlias(Guid.NewGuid(), Guid.NewGuid());

        sub.Should().StartWith("Submitter #").And.HaveLength("Submitter #".Length + 4);
        rev.Should().StartWith("Reviewer #").And.HaveLength("Reviewer #".Length + 4);
    }
}
