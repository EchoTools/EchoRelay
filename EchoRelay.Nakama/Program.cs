using CommandLine;
using EchoRelay.Core.Server;
using EchoRelay.Core.Server.Messages;
using EchoRelay.Core.Server.Services;
using EchoRelay.Core.Server.Storage;
using EchoRelay.Core.Server.Storage.Nakama;
using EchoRelay.Core.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using EchoRelay.Core.Server.Messages.Login;
using EchoRelay.Core.Server.Storage.Types;
//using EchoRelay.Nakama.Client;

namespace EchoRelay.Nakama
{
    class Program
    {
        /// <summary>
        /// The parsed CLI argument options for the application.
        /// </summary>
        private static CliOptions? Options;
        /// <summary>
        /// The instance of the server hosting central services.
        /// </summary>
        private static Server Server;
        /// <summary>
        /// The update timer used to trigger a peer stats update on a given interval.
        /// </summary>
        private static System.Timers.Timer? peerStatsUpdateTimer;
        /// <summary>
        /// The time that the server was started.
        /// </summary>
        private static DateTime startedTime;
        /// <summary>
        /// A mutex lock object to be used when printing to console, to avoid color collisions.
        /// </summary>
        private static object _printLock = new object();

        /// <summary>
        /// The CLI argument options for the application.
        /// </summary>
        public class CliOptions
        {
            [Option("nakama-uri", Required = true, Default = "http://localhost:7350/", HelpText = "The URI of the Nakama server.")]
            public string NakamaUri { get; set; } = "";

            [Option("nakama-serverkey", Required = false, HelpText = "The Nakama server key.")]
            public string? NakamaServerKey { get; set; } = "";

            [Option("nakama-deviceid", Required = false, HelpText = "The Nakama device ID to authenticate with.")]
            public string? NakamaDeviceId { get; set; } = "";

            [Option('g', "game", Required = false, HelpText = "The optional path to the game (echovr.exe). Extracts symbols to the server's symbol cache during initial deployment.")]
            public string? GameExecutablePath { get; set; }

            [Option('p', "port", Required = false, Default = 777, HelpText = "The TCP port to broadcast central services over.")]
            public int Port { get; set; }

            [Option("apikey", Required = false, Default = null, HelpText = "Requires a specific API key as part of the ServerDB connection URI query parameters.")]
            public string? ServerDBApiKey { get; set; }

            [Option("forcematching", Required = false, Default = true, HelpText = "Forces users to match to any available game, in the event of their requested game servers being unavailable.")]
            public bool ForceMatching { get; set; }

            [Option("lowpingmatching", Required = false, Default = false, HelpText = "Sets a preference for matching to game servers with low ping instead of high population.")]
            public bool LowPingMatching { get; set; }

            [Option("outputconfig", Required = false, HelpText = "Outputs the generated service config file to a given file path on disk.")]
            public string? OutputConfigPath { get; set; } = null;

            [Option("statsinterval", Required = false, Default = 3000, HelpText = "Sets the interval at which the CLI will output its peer stats (in milliseconds).")]
            public double StatsUpdateInterval { get; set; }

            [Option("noservervalidation", Required = false, Default = false, HelpText = "Disables validation of game servers using raw ping requests, ensuring their ports are exposed.")]
            public bool ServerDBValidateGameServers { get; set; }

            [Option("servervalidationtimeout", Required = false, Default = 3000, HelpText = "Sets the timeout for game server validation using raw ping requests. In milliseconds.")]
            public int ServerDBValidateGameServersTimeout { get; set; }

            [Option('v', "verbose", Required = false, Default = false, HelpText = "Output all data to console/file (includes debug output). ")]
            public bool Verbose { get; set; } = true;

            [Option('V', "debug", Required = false, Default = false, HelpText = "Output all client/server messages.")]
            public bool Debug { get; set; } = true;

            [Option('l', "logfile", Required = false, Default = null, HelpText = "Specifies the path to the log file.")]
            public string? LogFilePath { get; set; }

            [Option("disable-cache", Required = false, Default = false, HelpText = "Disables the file cache. Edits to JSON files will be immediately effective.")]
            public bool DisableCache { get; set; } = true;

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments the application was invoked with.</param>
        static async Task Main(string[] args) =>

            // Parse our command line arguments.
            await Parser.Default.ParseArguments<CliOptions>(args).WithParsedAsync(async options =>
            {
                // Set our options globally
                Options = options;

                // Verify other arguments
                if (options.Port < 0 || options.Port > ushort.MaxValue)
                {
                    Log.Fatal($"Provided listen port is invalid. You must a value between 1 and {ushort.MaxValue}.");
                    return;
                }
                else
                {
                    Log.Information($"[SERVER] Listening on port {options.Port}");
                }

                // Validate the Nakama URI
                Uri _nakamaUri;
                try
                {
                    _nakamaUri = new(options.NakamaUri);
                }
                catch (Exception ex)
                {
                    Log.Fatal($"Provided Nakama URI is invalid: {ex.Message}.");
                    return;
                }

                var _serverKey = QueryHelpers.ParseQuery(_nakamaUri.Query).GetValueOrDefault("serverKey", Options.NakamaServerKey);
                if (String.IsNullOrEmpty(_serverKey))
                {
                    Log.Fatal($"Server key must be provided in Nakama URI (e.g. ?serverKey=...) or via '-nakama-serverkey' argument.");
                    return;
                }

                var _relayId = QueryHelpers.ParseQuery(_nakamaUri.Query).GetValueOrDefault("relayId", Options.NakamaServerKey);
                if (String.IsNullOrEmpty(_relayId))
                {
                    Log.Fatal($"Relay identifier must be provided in Nakama URI (e.g. ?relayId=...) or via '-nakama-deviceid' argument.");
                    return;
                }
                else
                {
                    _relayId = Regex.Replace(_relayId, "^(?:RLY-)?(?<id>[-A-z0-9_]+)", "RLY-${id}");
                    Log.Information($"Authenticating with relayId: {_relayId}");
                }

                ConfigureLogger(options);

                Log.Debug($"Runtime arguments: '{string.Join(" ", args)}'");

                // Setup the Nakama connection

                NakamaServerStorage? serverStorage = null;
                try
                {
                    serverStorage = await NakamaServerStorage.ConnectNakamaStorageAsync(_nakamaUri.Scheme, _nakamaUri.Host, _nakamaUri.Port, _serverKey, _relayId);
                }
                catch (Exception ex)
                {
                    Log.Fatal($"Could not connect Nakama API: ${ex.Message}");
                    return;
                }

                serverStorage.Open();

                // Ensure the required resources are initialized.
                if (!serverStorage.AccessControlList.Exists())
                {
                    Log.Warning("[SERVER] Access Control Lists objects do not exist. Creating...");
                    InitialDeployment.DeployAccessControlList(serverStorage);
                }

                if (!serverStorage.ChannelInfo.Exists())
                {
                    Log.Warning("[SERVER] Channel Info objects do not exist. Creating...");
                    InitialDeployment.DeployChannelInfo(serverStorage);
                }

                if (!serverStorage.Configs.Exists(("main_menu", "main_menu")))
                {
                    Log.Warning("[SERVER] Configs objects do not exist. Creating...");
                    InitialDeployment.DeployConfigs(serverStorage);
                }

                if (!serverStorage.Documents.Exists(("eula", "en")))
                {
                    Log.Warning("[SERVER] Document objects do not exist. Creating...");
                    InitialDeployment.DeployDocuments(serverStorage);
                }
                if (!serverStorage.LoginSettings.Exists())
                {
                    Log.Warning("[SERVER] Login Settings do not exist. Creating...");
                    InitialDeployment.DeployLoginSettings(serverStorage);
                }
                if (!serverStorage.SymbolCache.Exists())
                {
                    Log.Warning("[SERVER] Symbol Cache does not exist. Creating...");
                    InitialDeployment.DeploySymbolCache(serverStorage);
                }

                // Create a server instance
                Server = new Server(serverStorage,
                    new ServerSettings(
                        port: (ushort)options.Port,
                        serverDbApiKey: options.ServerDBApiKey,
                        serverDBValidateServerEndpoint: options.ServerDBValidateGameServers,
                        serverDBValidateServerEndpointTimeout: options.ServerDBValidateGameServersTimeout,
                        favorPopulationOverPing: !options.LowPingMatching,
                        forceIntoAnySessionIfCreationFails: options.ForceMatching
                        )
                    );

                // Set up all event handlers.
                Server.OnServerStarted += Server_OnServerStarted;
                Server.OnServerStopped += Server_OnServerStopped;
                Server.OnAuthorizationResult += Server_OnAuthorizationResult;
                Server.OnServicePeerConnected += Server_OnServicePeerConnected;
                Server.OnServicePeerDisconnected += Server_OnServicePeerDisconnected;
                Server.OnServicePeerAuthenticated += Server_OnServicePeerAuthenticated;
                Server.ServerDBService.Registry.OnGameServerRegistered += Registry_OnGameServerRegistered;
                Server.ServerDBService.Registry.OnGameServerUnregistered += Registry_OnGameServerUnregistered;
                Server.ServerDBService.OnGameServerRegistrationFailure += ServerDBService_OnGameServerRegistrationFailure;

                // Set up all verbose event handlers.
                if (options.Debug || options.Verbose)
                {
                    Server.OnServicePacketSent += Server_OnServicePacketSent;
                    Server.OnServicePacketReceived += Server_OnServicePacketReceived;
                }

                // Setup metrics forwarding
                //Server.OnServicePacketReceived += Metrics.Server_OnServicePacketReceived;


                await Server.Start();
            });

        private static void ConfigureLogger(CliOptions options)
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code);

            if (options.LogFilePath != null)
            {
                logConfig.WriteTo.File(
                    path: Options.LogFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            logConfig = options.Verbose
                ? logConfig.MinimumLevel.Verbose()
                : options.Debug
                    ? logConfig.MinimumLevel.Debug()
                    : logConfig.MinimumLevel.Information();

            Log.Logger = logConfig.CreateLogger();
        }

        private static void Server_OnServerStarted(Server server)
        {
            // Print our server started message
            Log.Information("[SERVER] Server started");

            // Print our service config.
            Core.Game.ServiceConfig serviceConfig = server.Settings.GenerateServiceConfig(server.PublicIPAddress?.ToString() ?? "localhost", serverConfig: true);
            string serviceConfigSerialized = JsonConvert.SerializeObject(serviceConfig, Formatting.Indented, StreamIO.JsonSerializerSettings);
            Log.Information($"[SERVER] Generated service config:\n{serviceConfigSerialized}");

            // Copy the service config to the clipboard if required.
            if (Options?.OutputConfigPath != null)
            {
                // Save the service config to the provided file path.
                try
                {
                    File.WriteAllText(Options!.OutputConfigPath, serviceConfigSerialized);
                    Log.Information($"[SERVER] Output generated service config to path \"{Options!.OutputConfigPath}\"");
                }
                catch (Exception ex)
                {
                    Log.Error($"[SERVER] Failed to output generated service config to path \"{Options!.OutputConfigPath}\":\n{ex}");
                }
            }

            // Start the peer stats update timer
            peerStatsUpdateTimer = new System.Timers.Timer(Options!.StatsUpdateInterval);
            peerStatsUpdateTimer.Start();
            peerStatsUpdateTimer.Elapsed += PeerStatsUpdateTimer_Elapsed;
        }

        private static void PeerStatsUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Log.Information($"[PEERSTATS] " +
                $"gameservers: {Server.ServerDBService.Registry.RegisteredGameServers.Count}, " +
                $"login: {Server.LoginService.Peers.Count}, " +
                $"config: {Server.ConfigService.Peers.Count}, " +
                $"matching: {Server.MatchingService.Peers.Count}, " +
                $"serverdb: {Server.ServerDBService.Peers.Count}, " +
                $"transaction: {Server.TransactionService.Peers.Count}"
                );
        }

        private static void Server_OnServerStopped(Server server)
        {
            // Stop the update timer.
            peerStatsUpdateTimer?.Stop();

            // Print our server stopped message
            Log.Information("[SERVER] Server stopped");
            Log.CloseAndFlush();
        }

        private static void Server_OnAuthorizationResult(Server server, System.Net.IPEndPoint client, bool authorized)
        {
            if (!authorized)
                Log.Information($"[SERVER] client({client.Address}:{client.Port}) failed authorization");
        }

        private static void Server_OnServicePeerConnected(Core.Server.Services.Service service, Core.Server.Services.Peer peer)
        {
            Log.Debug($"[{service.Name}] client({peer.Address}:{peer.Port}) connected");
        }

        private static void Server_OnServicePeerDisconnected(Core.Server.Services.Service service, Core.Server.Services.Peer peer)
        {
            Log.Debug($"[{service.Name}] client({peer.Address}:{peer.Port}) disconnected");
        }

        private static void Server_OnServicePeerAuthenticated(Core.Server.Services.Service service, Core.Server.Services.Peer peer, Core.Game.XPlatformId userId)
        {
            Log.Information($"[{service.Name}] client({peer.Address}:{peer.Port}) authenticated as account='{userId}' displayName='{peer.UserDisplayName}'");
        }

        private static void Registry_OnGameServerRegistered(Core.Server.Services.ServerDB.RegisteredGameServer gameServer)
        {
            Log.Information($"[{gameServer.Peer.Service.Name}] client({gameServer.Peer.Address}:{gameServer.Peer.Port}) registered game server (server_id={gameServer.ServerId}, region_symbol={gameServer.RegionSymbol}, version_lock={gameServer.VersionLock}, endpoint=<{gameServer.ExternalAddress}:{gameServer.Port}>)");
        }

        private static void Registry_OnGameServerUnregistered(Core.Server.Services.ServerDB.RegisteredGameServer gameServer)
        {
            Log.Information($"[{gameServer.Peer.Service.Name}] client({gameServer.Peer.Address}:{gameServer.Peer.Port}) unregistered game server (server_id={gameServer.ServerId}, region_symbol={gameServer.RegionSymbol}, version_lock={gameServer.VersionLock}, endpoint=<{gameServer.ExternalAddress}:{gameServer.Port}>)");
        }

        private static void ServerDBService_OnGameServerRegistrationFailure(Peer peer, Core.Server.Messages.ServerDB.ERGameServerRegistrationRequest registrationRequest, string failureMessage)
        {
            Log.Error($"[{peer.Service.Name}] client({peer.Address}:{peer.Port}) failed to register game server: \"{failureMessage}\"");
        }

        private static void Server_OnServicePacketSent(Core.Server.Services.Service service, Core.Server.Services.Peer sender, Core.Server.Messages.Packet packet)
        {
            packet.ForEach(p => Log.Debug($"[{service.Name}] ({sender.Address}:{sender.Port}) SENT: " + p));
        }

        private static void Server_OnServicePacketReceived(Core.Server.Services.Service service, Core.Server.Services.Peer sender, Core.Server.Messages.Packet packet)
        {
            packet.ForEach(p => Log.Debug($"[{service.Name}] ({sender.Address}:{sender.Port}) RECV: " + p));
        }
    }
}
