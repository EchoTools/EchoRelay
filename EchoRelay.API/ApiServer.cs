using System.Text;
using EchoRelay.API.Public;
using EchoRelay.Core.Server;
using Newtonsoft.Json;
using Serilog;

namespace EchoRelay.API
{
    public class ApiServer
    {
        public static ApiServer? Instance { get; private set; }

        public Server RelayServer { get; private set; }

        public delegate void ApiSettingsUpdated();
        public event ApiSettingsUpdated? OnApiSettingsUpdated;
        public ApiSettings ApiSettings { get; private set; }

        public ApiServer(Server relayServer, ApiSettings apiSettings)
        {
            Instance = this;

            RelayServer = relayServer;
            ApiSettings = apiSettings;

            var builder = WebApplication.CreateBuilder();
            builder.Services.AddCors(options =>
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                )
            );
            builder.Services.AddControllers().AddApplicationPart(typeof(ApiServer).Assembly).AddNewtonsoftJson();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Host.UseSerilog();

            var app = builder.Build();
            app.UseCors("AllowAll");

            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

            // Reduce logging noise
            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger().UseSwaggerUI();
            }

            app.UseWhen(context => context.Request.Path.StartsWithSegments("/centralapi"), branch =>
            {
                branch.UseMiddleware<PublicApiAuthentication>();
            });

            app.UseWhen(context => !context.Request.Path.StartsWithSegments("/centralapi"), branch =>
            {
                branch.UseMiddleware<ApiAuthentication>();
            });

            app.UseAuthorization();
            app.MapControllers();

            if (ApiSettings.CentralApiUrl != null)
            {
                // Update registration state on startup/shutdown
                lifetime.ApplicationStarted.Register(async () => await SendCentralApiRegistrationUpdate(true));
                lifetime.ApplicationStopping.Register(async () => await SendCentralApiRegistrationUpdate(false));
            }

            app.RunAsync("http://0.0.0.0:8080");
        }

        public void UpdateApiSettings(ApiSettings newSettings)
        {
            ApiSettings = newSettings;
            OnApiSettingsUpdated?.Invoke();
        }

        public async Task SendCentralApiRegistrationUpdate(bool isOnline)
        {
            if (ApiSettings.CentralApiUrl == null)
            {
                return;
            }
            try
            {
                using (HttpClient httpClient = new() { 
                    BaseAddress = new Uri(ApiSettings.CentralApiUrl)
                })
                {
                    httpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiSettings.CentralApiKey);

                    var requestData = new PublicServerInfo(RelayServer, isOnline);
                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"api/setServerStatus/{RelayServer.PublicIPAddress}", content);
                    response.EnsureSuccessStatusCode();

                    Log.Information("Registered on Central API as '{0}:'", RelayServer.PublicIPAddress);
                }
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case HttpRequestException _:
                    default:
                        Log.Warning("Error registering as {0} on central API: {1}", RelayServer.PublicIPAddress, ex.Message);
                        break;
                }
            }
        }
    }
}
