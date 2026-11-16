namespace Throughline.SmokeTests;

internal sealed record SmokeTestSettings(
    Uri BaseUrl,
    string TenantId,
    string Scope,
    string ClientId,
    string ClientSecret);