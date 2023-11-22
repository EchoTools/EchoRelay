namespace EchoRelay.API
{
    public class ApiAuthentication
    {
        static string? ApiKey => ApiServer.Instance?.ApiSettings.ApiKey;
        private const string ApiKeyHeaderName = "X-Api-Key";
        private readonly RequestDelegate _next;

        public ApiAuthentication(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (ApiKey == null)
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var RequestApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key missing");
                return;
            }

            if (ApiKey != RequestApiKey.First())
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            await _next(context);
        }
    }
}
