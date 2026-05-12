namespace InnovatEpam.Portal.Application.Auth.Dtos;

/// <summary>Self-service registration request (FR-002). New users are created with the <c>Submitter</c> role.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Login request (FR-006).</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Authentication response containing the access token and the authenticated user.</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserSummary User);

/// <summary>Lightweight user projection returned alongside the JWT.</summary>
public sealed record UserSummary(Guid Id, string Email, string DisplayName, string Role);
