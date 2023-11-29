using System.Text;
using EchoRelay.API.Controllers;
using EchoRelay.Core.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
            HttpClient.BaseAddress = new Uri(ApiSettings.Advertise);

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
            
            // Reduce logging noise
            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/servers"), branch =>
            {
                branch.UseMiddleware<ApiAuthentication>();
            });
            
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/sessions"), branch =>
            {
                branch.UseMiddleware<ApiAuthentication>();
            });
            
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/accounts"), branch =>
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
        public async void registerServiceOnCentralAPI(bool online)
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
                Console.WriteLine(apiUrl);
                Console.WriteLine(jsonData);
                Console.WriteLine(RelayServer);
                Console.WriteLine(RelayServer.PublicIPAddress);
                // Perform the POST request
                var response = await HttpClient.PostAsync(apiUrl, content);

                // Check if the request was successful (2xx status)
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred during the request: {ex.Message}");
            }
        }
    }
}
