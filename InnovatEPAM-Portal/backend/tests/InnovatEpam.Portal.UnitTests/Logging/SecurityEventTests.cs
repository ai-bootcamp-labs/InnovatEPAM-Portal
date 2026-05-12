using FluentAssertions;
using InnovatEpam.Portal.Api.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace InnovatEpam.Portal.UnitTests.Logging;

/// <summary>
/// T113 / T113b — verifies the security event contract:
/// <list type="bullet">
///   <item>Sensitive properties (password, accessToken, …) are masked, never written verbatim.</item>
///   <item>The five canonical event names emit correctly through Serilog.</item>
/// </list>
/// </summary>
public class SecurityEventTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static (Logger logger, CapturingSink sink) BuildLogger()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Destructure.With<SensitivePropertyScrubbingPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();
        return (logger, sink);
    }

    private sealed record CredentialPayload(string Email, string Password, string AccessToken);

    [Fact]
    public void Sensitive_property_values_are_masked_when_destructured()
    {
        var (logger, sink) = BuildLogger();
        try
        {
            var payload = new CredentialPayload("user@example.com", "S3cret!Pass", "jwt.token.value");
            logger.Information("auth.test {@Payload}", payload);

            var rendered = sink.Events.Should().ContainSingle().Subject.RenderMessage();
            rendered.Should().NotContain("S3cret!Pass");
            rendered.Should().NotContain("jwt.token.value");
            rendered.Should().Contain("***");
            rendered.Should().Contain("user@example.com");
        }
        finally { logger.Dispose(); }
    }

    [Theory]
    [InlineData("auth.login.success")]
    [InlineData("auth.login.failure")]
    [InlineData("auth.logout")]
    [InlineData("auth.register.success")]
    [InlineData("decision.recorded")]
    [InlineData("decision.conflict")]
    public void Canonical_event_names_are_preserved_in_message_template(string eventName)
    {
        var (logger, sink) = BuildLogger();
        try
        {
            logger.Information("{EventName} {Payload}", eventName, "context");
            var evt = sink.Events.Should().ContainSingle().Subject;
            evt.Properties["EventName"].ToString().Should().Contain(eventName);
        }
        finally { logger.Dispose(); }
    }

    [Fact]
    public void Scrub_policy_recognises_all_documented_property_names()
    {
        SensitivePropertyScrubbingPolicy.IsSensitive("password").Should().BeTrue();
        SensitivePropertyScrubbingPolicy.IsSensitive("Password").Should().BeTrue();
        SensitivePropertyScrubbingPolicy.IsSensitive("accessToken").Should().BeTrue();
        SensitivePropertyScrubbingPolicy.IsSensitive("authorization").Should().BeTrue();
        SensitivePropertyScrubbingPolicy.IsSensitive("displayName").Should().BeFalse();
        _ = new MessageTemplateParser(); // keeps Serilog.Parsing referenced for clarity
    }
}
