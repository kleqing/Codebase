using Microsoft.Extensions.Configuration;

namespace YourApp.Shared.Utils;

public class UrlHelper
{
    public static string GetBackendUrl(IConfiguration configuration) 
        => Environment.GetEnvironmentVariable("BACKEND_URL") ?? configuration["URLs:BackendURL"] ?? string.Empty;
    
    public static string GetFrontendUrl(IConfiguration configuration) 
        => Environment.GetEnvironmentVariable("FRONTEND_URL") ?? configuration["URLs:FrontendURL"] ?? string.Empty;
}