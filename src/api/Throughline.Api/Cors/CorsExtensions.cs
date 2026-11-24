namespace Throughline.Api.Cors;

internal static class CorsExtensions
{
    private const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Lets the web app call the API from its own address. With no origins configured, no browser
    /// page on another address can call the API.
    /// </summary>
    public static IServiceCollection AddThroughlineCors(
        this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];

        // Bearer tokens travel in the Authorization header, not cookies, so credentials stay off.
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
