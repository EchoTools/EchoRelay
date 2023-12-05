using System.Text;
using EchoRelay.API.Public;
using EchoRelay.Core.Server;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Newtonsoft.Json;
using Serilog;

namespace EchoRelay.API
{
    public class ApiServer
    {
        public static ApiServer? Instance;

        public Server RelayServer { get; private set; }

        public delegate void ApiSettingsUpdated();
        public event ApiSettingsUpdated? OnApiSettingsUpdated;
        public ApiSettings ApiSettings { get; private set; }
        
        public HttpClient HttpClient;

        public ApiServer(Server relayServer, ApiSettings apiSettings)
        {
            Instance = this;

            RelayServer = relayServer;
            ApiSettings = apiSettings;
            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new Uri(ApiSettings.NotifyCentralApi);

            var builder = WebApplication.CreateBuilder();
            builder.Services.AddCors(options =>
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                )
            );
            builder.Services.AddControllers().AddApplicationPart(typeof(ApiServer).Assembly);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Host.UseSerilog();

            var app = builder.Build();
            app.UseCors("AllowAll");
            
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

            // Register a callback for the ApplicationStopping event
            lifetime.ApplicationStopping.Register(() =>
            {
                // Your shutdown logic or logging here
                if(apiSettings.NotifyCentralApi != null)
                    registerServiceOnCentralAPI(false);
            });
            
            lifetime.ApplicationStarted.Register(() =>
            {
                // Your startup logic or logging here
                if(apiSettings.NotifyCentralApi != null)
                    registerServiceOnCentralAPI(true);
            });
            
            // Reduce logging noise
            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/centralApi"), branch =>
            {
                branch.UseMiddleware<PublicApiAuthentication>();
            });
            
            app.UseWhen(context => !context.Request.Path.StartsWithSegments("/centralApi"), branch =>
            {
                branch.UseMiddleware<ApiAuthentication>();
            });

            app.UseAuthorization();
            app.MapControllers();

            app.RunAsync("http://0.0.0.0:8080");
        }

        public void UpdateApiSettings(ApiSettings newSettings)
        {
            ApiSettings = newSettings;
            OnApiSettingsUpdated?.Invoke();
        }
        private async void registerServiceOnCentralAPI(bool online)
        {
            try
            {
                // Create the JSON data from your request model
                var requestData = new PublicServerInfo(RelayServer, online);
                var jsonData = JsonConvert.SerializeObject(requestData);

                // Create the content for the POST request using JSON data
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Specify the URL of the external API
                var apiUrl = $"api/setServerStatus/{RelayServer.PublicIPAddress}";

                // Add the X-Api-Key header
                HttpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiSettings.CentralApiKey); // Replace "your-api-key" with the actual API key

                // Perform the POST request
                var response = await HttpClient.PostAsync(apiUrl, content);

                // Check if the request was successful (2xx status)
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred during the request: {ex.Message}");
            }
            finally
            {
                // Make sure to remove the header after the request to avoid unintended side effects
                HttpClient.DefaultRequestHeaders.Remove("X-Api-Key");
            }
        }
    }
}
