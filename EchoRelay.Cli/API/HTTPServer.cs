using EchoRelay.Cli.Utils;
using EchoRelay.Core.Server.Storage.Types;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static EchoRelay.Cli.Program;

namespace EchoRelay.Cli.API
{
    public static class HTTPServer
    {
        public static void Server_StartHTTPAPI()
        {
            Info("[HTTPSERVER] Starting HTTP API");
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://0.0.0.0:{Program.Options?.HTTPPort}/");

            listener.Start();

            Console.WriteLine($" {Program.Options?.HTTPPort}...");
            Info($"[HTTPSERVER] Successfully started HTTP API on port {Program.Options?.HTTPPort}!");

            while (true)
            {
                HttpListenerContext ctx = listener.GetContext();
                HttpListenerRequest req = ctx.Request;

                string? path = ctx.Request.Url?.LocalPath;

                if (path != null && path.ToLower().StartsWith("/ban") && req.HttpMethod == "POST" && req.ContentType == "application/x-www-form-urlencoded")
                {
                    string username = GetFormDataValue(req, "username");
                    AccountResource account = AccountUtils.GetAccount(username);
                    TimeSpan time = GetFromTimeFrameString(GetFormDataValue(req, "time"));

                    bool banSuccess = AccountUtils.Ban(account, time);
                    string jsonResponse = banSuccess
                        ? "{\"error\": null, \"success\": true}"
                        : "{\"error\": \"Ban was unsuccessful\", \"success\": false}";

                    SendJsonResponse(ctx.Response, jsonResponse);
                }
                if (path != null && path.ToLower().StartsWith("/kick") && req.HttpMethod == "POST" && req.ContentType == "application/x-www-form-urlencoded")
                {
                    Task.Run(() => HandleKickAsync(ctx, req));
                }
                if (path != null && path.ToLower().StartsWith("/gameservers"))
                {
                    if (req.HttpMethod != "GET")
                    {
                        ctx.Response.StatusCode = 405;
                        ctx.Response.Close();
                    }
                    else
                    {

                    }
                }
                else
                {
                    Info($"Request for '{path}' couldn't be completed since there's literally nothing to do with it.");
                }
            }
        }

        public static async void HandleKickAsync(HttpListenerContext ctx, HttpListenerRequest req)
        {
            string username = GetFormDataValue(req, "username");
            AccountResource account = AccountUtils.GetAccount(username);

            bool kickSuccess = await AccountUtils.Kick(account);
            string jsonResponse = kickSuccess
                ? "{\"error\": null, \"success\": true}"
                : "{\"error\": \"Kick was unsuccessful\", \"success\": false}";

            SendJsonResponse(ctx.Response, jsonResponse);
        }

        public static TimeSpan GetFromTimeFrameString(string timeFrameString)
        {
            var period = int.Parse(timeFrameString.Remove(timeFrameString.Length - 1, 1));
            var timeType = timeFrameString.Substring(timeFrameString.Length - 1, 1);

            return timeType switch
            {
                "h" => TimeSpan.FromHours(period),
                "d" => TimeSpan.FromDays(period),
                "m" => TimeSpan.FromDays(period * 30),
                _ => throw new Exception("No possible time frame given! Possible time frames = h (hours), d (days), m (months)")
            };
        }

        static string GetFormDataValue(HttpListenerRequest request, string key)
        {
            using (Stream body = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
                {
                    string formData = reader.ReadToEnd();
                    var formDataPairs = formData.Split('&');

                    foreach (var pair in formDataPairs)
                    {
                        var keyValue = pair.Split('=');
                        if (keyValue.Length == 2 && keyValue[0] == key)
                        {
                            return Uri.UnescapeDataString(keyValue[1]);
                        }
                    }
                }
            }

            return string.Empty;
        }
        static void SendJsonResponse(HttpListenerResponse response, string jsonResponse)
        {
            response.ContentType = "application/json";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        static void ReturnStatusCode()
        {

        }
    }
}
