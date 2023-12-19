using CommandLine;
using EchoRelay.API;
using EchoRelay.Core.Server;
using EchoRelay.Core.Server.Services;
using EchoRelay.Core.Server.Services.Login;
using EchoRelay.Core.Server.Storage;
using EchoRelay.Core.Server.Storage.Filesystem;
using EchoRelay.Core.Utils;
using EchoRelay.Nakama;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Text.RegularExpressions;
namespace EchoRelay.Cli
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

        public static EchoNakama nakamaClient { get; private set; }

        /// <summary>
        /// The CLI argument options for the application.
        /// </summary>
        private static ApiServer? ApiServer;

        public class CliOptions
        {
            [Option('d', "database", SetName = "filesystem", Required = false, HelpText = "specify database folder")]
            public string? DatabasePath { get; set; }

            [Option('N', "nakama-uri", SetName = "nakama", Required = false, HelpText = "specify Nakama server URI. (e.g. http://localhost:7351?server_key=...&relay_id=...)")]
            public string? NakamaUri { get; set; }

            [Option('g', "game", Required = false, HelpText = "specify path to the 'ready-at-dawn-echo-arena' for building the symbol cache")]
            public string? GameBasePath { get; set; }

            [Option('p', "port", Required = false, Default = 7777, HelpText = "specify the TCP port to listen on")]
            public int Port { get; set; }

            [Option("apikey", Required = false, Default = null, HelpText = "require game servers authenticate with API Key (via '?apikey=' query parameters).")]
            public string? ServerDBApiKey { get; set; }

            [Option("forcematching", Required = false, Default = true, HelpText = "attempt to match player again if first match fails.")]
            public bool ForceMatching { get; set; }

            [Option("lowpingmatching", Required = false, Default = false, HelpText = "prefer matches on lower ping game servers vs higher population.")]
            public bool LowPingMatching { get; set; }

            [Option("outputconfig", Required = false, HelpText = "specify the path to write an example 'config.json'.")]
            public string? OutputConfigPath { get; set; } = null;

            [Option("statsinterval", Required = false, Default = 3000, HelpText = "specify update interval for peer stats output (in milliseconds).")]
            public double StatsUpdateInterval { get; set; }

            [Option("noservervalidation", Required = false, Default = false, HelpText = "disable validation of game server connectivity.")]
            public bool ServerDBValidateGameServers { get; set; }

            [Option("servervalidationtimeout", Required = false, Default = 3000, HelpText = "set game server validation timeout for game server validation using raw ping requests. In milliseconds.")]
            public int ServerDBValidateGameServersTimeout { get; set; }

            [Option('v', "verbose", Required = false, Default = false, HelpText = "increase verbosity")]
            public bool Verbose { get; set; } = true;

            [Option('V', "debug", Required = false, Default = false, HelpText = "emit debugging output")]
            public bool Debug { get; set; } = true;

            [Option('l', "logfile", Required = false, Default = null, HelpText = "send output to a logfile")]
            public string? LogFilePath { get; set; }

            [Option("disable-cache", Required = false, Default = false, HelpText = "disable caching of database resources, file edits are immediately effective")]
            public bool DisableCache { get; set; } = true;

            [Option("enable-api", Required = false, Default = false, HelpText = "enable the API server")]
            public bool EnableApi { get; set; } = true;

            [Option("central-api-key", Required = false, Default = null, HelpText = "authenticate to central api using key")]
            public string? CentralApiKey { get; set; }
            
            [Option("notify-central-api", Required = false, HelpText = "notify central api when this relay is online at URL")]
            public string? CentralApiUrl { get; set; } = null;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments the application was invoked with.</param>
        static async Task Main(string[] args)
        {
            try
            {
                // Parse our command line arguments.
                await Parser.Default.ParseArguments<CliOptions>(args).WithParsedAsync(async options =>
                {
                    // Set our options globally
                    Options = options;
                    IServerStorage serverStorage;

                    if (options.Port < 0 || options.Port > ushort.MaxValue)
                        throw new ArgumentException($"Invalid port: '{options.Port}' Port must be between 1 and {ushort.MaxValue}");

                    ConfigureLogger(options);

                    Log.Debug($"Runtime arguments: '{string.Join(" ", args)}'");

                    if (Options.NakamaUri != null)
                    {
                        // Use Nakama for storage
                        nakamaClient = await ConfigureNakamaAsync(Options.NakamaUri);
                        serverStorage = await NakamaServerStorage.ConnectNakamaStorageAsync(nakamaClient);
                    }
                    else if (Options.DatabasePath != null)
                    {
                        // Use the filesystem for storage
                        if (!Directory.Exists(options.DatabasePath))
                            throw new ArgumentException($"Database directory does not exist: '{options.DatabasePath}'");

                        // Create our file system storage and open it.
                        serverStorage = new FilesystemServerStorage(options.DatabasePath, Options.DisableCache);
                    }
                    else
                    {
                        throw new ArgumentException("Either '--database' or '--nakama-uri' must be specified.");
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
                        Log.Warning("[SERVER] Config objects do not exist. Creating...");
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
                        InitialDeployment.DeploySymbolCache(serverStorage, options.GameBasePath);
                    }

                    var serverSettings = new ServerSettings(
                                port: (ushort)options.Port,
                                serverDbApiKey: options.ServerDBApiKey,
                                serverDBValidateServerEndpoint: options.ServerDBValidateGameServers,
                                serverDBValidateServerEndpointTimeout: options.ServerDBValidateGameServersTimeout,
                                favorPopulationOverPing: !options.LowPingMatching,
                                forceIntoAnySessionIfCreationFails: options.ForceMatching
                                );

                    if (Options.NakamaUri != null)
                    {
                        // Create a server instance
                        Server = new Server(serverStorage,
                                            serverSettings,
                                            loginServiceFactory: server => new NLoginService(server, nakamaClient));
                            
                    } else
                    {
                        // Create a server instance
                        Server = new Server(serverStorage, serverSettings);
                    }
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


                    // Start the server.
                    await Server.Start();
                });
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case System.Net.HttpListenerException httpListenerException:
                        if (httpListenerException.ErrorCode == 5)
                            Log.Information("The requested operation requires elevation (Run as administrator).\n\n"
                           + $"To run as a user, execute 'netsh http add urlacl url=http://*:{Options.Port}/ user=Everyone' as Administrator");

                        Log.Fatal($"Unable to start listener for connections: {ex.Message}");
                        Console.WriteLine($"Invalid Argument: {ex.Message}");
                        Environment.Exit(0xA0);
                        break;

                    case ArgumentException argumentException:
                        Console.WriteLine($"Invalid Argument: {ex.Message}");
                        Environment.Exit(0xA0);
                        break;

                    default:
                        throw;
                }
            };
        }
        /// <summary>
        /// Configures the Serilog logger based on the provided command-line options.
        /// </summary>
        /// <param name="options">The command-line options specifying the logging configuration.</param>
        private static void ConfigureLogger(CliOptions options)
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.Async(a => a.Console(theme: AnsiConsoleTheme.Code));

            if (options.LogFilePath != null)
            {
                logConfig.WriteTo.Async(a => a.File(
                    path: options.LogFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));
            }

            logConfig = options.Verbose
                ? logConfig.MinimumLevel.Verbose()
                : options.Debug
                    ? logConfig.MinimumLevel.Debug()
                    : logConfig.MinimumLevel.Information();

            // reduce the noise of the API Server
            logConfig.MinimumLevel.Override("Microsoft.AspNetCore",
                options.Verbose ? LogEventLevel.Debug : LogEventLevel.Warning);

            Log.Logger = logConfig.CreateLogger();
        }

        /// <summary>
        /// Configures and connects to the Nakama backend for server storage.
        /// </summary>
        /// <param name="nakamaUriString">The Nakama URI string specifying the Nakama server connection details.</param>
        /// <returns>An instance of <see cref="IServerStorage"/> representing the configured Nakama server storage.</returns>
        private static async Task<EchoNakama> ConfigureNakamaAsync(string nakamaUriString)
        {
            // Validate the Nakama URI
            Uri _nakamaUri;

            StringValues _serverKey;
            StringValues _relayId;
            try
            {
                _nakamaUri = new(nakamaUriString);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Provided Nakama URI is invalid: {ex.Message}.");
            }

            var _parsed = QueryHelpers.ParseQuery(_nakamaUri.Query);

            if (!_parsed.TryGetValue("server_key", out _serverKey))
                throw new ArgumentException($" 'server_key' be provided in uri query string. (e.g. ?server_key=...)");

            if (!_parsed.TryGetValue("relay_id", out _relayId))
            {
                Log.Warning($"No 'relayid' specified in Nakama URI query strings. Using machine name.");
                _relayId = System.Environment.MachineName;
            }
            _relayId = Regex.Replace(_relayId, "^(?<id>[-A-z0-9_]+)", "RELAY:${id}");

            Log.Information($"Authenticating with relayId: '{_relayId}'");

            // Configure the Nakama storage
            try
            {
                return new EchoNakama(_nakamaUri, _serverKey, _relayId);
               
                
            }
            catch (Exception ex)
            {
                Log.Fatal($"Could not connect Nakama API: ${ex.Message}");
                throw new ApplicationException($"Could not connect Nakama API: ${ex.Message}");
            }
        }

        private static void Server_OnServerStarted(Server server)
        {
            if (Options.EnableApi)
            {
                ApiServer = new ApiServer(server, new ApiSettings(apiKey: Options.ServerDBApiKey, centralApiUrl: Options.CentralApiUrl, centralApiKey:Options.CentralApiKey));
            }
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

        private static void Server_OnServicePeerConnected(Core.Server.Services.IService service, Core.Server.Services.Peer peer)
        {
            Log.Debug($"[{service.Name}] client({peer.Address}:{peer.Port}) connected");
        }

        private static void Server_OnServicePeerDisconnected(Core.Server.Services.IService service, Core.Server.Services.Peer peer)
        {
            Log.Debug($"[{service.Name}] client({peer.Address}:{peer.Port}) disconnected");
        }

        private static void Server_OnServicePeerAuthenticated(Core.Server.Services.IService service, Core.Server.Services.Peer peer, Core.Game.XPlatformId userId)
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

        private static void Server_OnServicePacketSent(Core.Server.Services.IService service, Core.Server.Services.Peer sender, Core.Server.Messages.Packet packet)
        {
            packet.ForEach(p => Log.Verbose($"[{service.Name}] ({sender.Address}:{sender.Port}) SENT: " + p));
        }

        private static void Server_OnServicePacketReceived(Core.Server.Services.IService service, Core.Server.Services.Peer sender, Core.Server.Messages.Packet packet)
        {
            packet.ForEach(p => Log.Verbose($"[{service.Name}] ({sender.Address}:{sender.Port}) RECV: " + p));
        }
    }
}
