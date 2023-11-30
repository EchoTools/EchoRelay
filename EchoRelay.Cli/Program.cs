using System.Runtime.InteropServices;
using CommandLine;
using EchoRelay.API;
using EchoRelay.Core.Server;
using EchoRelay.Core.Server.Services;
using EchoRelay.Core.Server.Storage;
using EchoRelay.Core.Server.Storage.Filesystem;
using EchoRelay.Core.Utils;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

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
        /// <summary>
        /// The CLI argument options for the application.
        /// </summary>
        private static ApiServer? ApiServer;


        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler();
        private static EventHandler? consoleCloseHandler;

        // Enum to represent different CtrlTypes
        
        public class CliOptions
        {
            [Option('d', "database", SetName = "filesystem", Required = false, HelpText = "specify database folder")]
            public string? DatabaseFolder { get; set; }

            [Option('g', "game", Required = false, HelpText = "specify path to the 'ready-at-dawn-echo-arena' for building the symbol cache")]
            public string? GameBasePath { get; set; }

            [Option('p', "port", Required = false, Default = 777, HelpText = "specify the TCP port to listen on")]
            public int Port { get; set; }

            [Option("apikey", Required = false, Default = null, HelpText = "require game servers authenticate with API Key (via '?apikey=' query parameters).")]
            public string? ServerDBApiKey { get; set; }
            
            [Option("centralapikey", Required = false, Default = null, HelpText = "require central api authenticate with API Key (via '?centralapikey=' query parameters).")]
            public string? CentralApiKey { get; set; }
            
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

            [Option("advertise", Required = false, Default = null,
                HelpText = "advertise central api when your relay is online")]
            public string? Advertise { get; set; } = null;

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments the application was invoked with.</param>
        static async Task Main(string[] args)
        {
            // Parse our command line arguments.
            await Parser.Default.ParseArguments<CliOptions>(args).WithParsedAsync(async options =>
            {
                // Set our options globally
                Options = options;

                ConfigureLogger(options);

                // Use the filesystem for storage
                if (!Directory.Exists(options.DatabaseFolder))
                {
                    Log.Warning($"Creating database directory: {options.DatabaseFolder}");
                    Directory.CreateDirectory(options.DatabaseFolder);
                }

                // Verify other arguments
                if (options.Port < 0 || options.Port > ushort.MaxValue)
                {
                    Log.Fatal($"Provided port is invalid. You must a value between 1 and {ushort.MaxValue}.");
                    return;
                }

                Log.Debug($"Runtime arguments: '{string.Join(" ", args)}'");
                // Create our file system storage and open it.
                ServerStorage serverStorage = new FilesystemServerStorage(options.DatabaseFolder, Options.DisableCache);

                serverStorage.Open();

                // Check if initial deployment needs to be performed.
                // If the database folder is empty, we deploy all resources.
                // If it is non-empty, but missing critical resources, we ask whether to clear all resources but accounts.
                bool allCriticalResourcesExist = serverStorage.AccessControlList.Exists() && serverStorage.ChannelInfo.Exists() && serverStorage.LoginSettings.Exists() && serverStorage.SymbolCache.Exists();
                bool anyCriticalResourcesExist = serverStorage.AccessControlList.Exists() || serverStorage.ChannelInfo.Exists() || serverStorage.LoginSettings.Exists() || serverStorage.SymbolCache.Exists();
                bool performInitialSetup = !allCriticalResourcesExist;
                if (performInitialSetup && anyCriticalResourcesExist)
                {
                    Log.Warning("Critical resources are missing from storage, but storage is non-empty.\n" +
                        "Would you like to re-deploy initial setup resources? Warning: this will clear all storage except accounts! [y/N]");
                    performInitialSetup = Console.ReadKey(true).Key == ConsoleKey.Y;
                }

                // Perform initial setup of server resources if needed.
                if (performInitialSetup)
                {
                    Log.Information("[SERVER] Performing initial setup: server resources to database folder..");
                    InitialDeployment.PerformInitialDeployment(serverStorage, options.GameBasePath, false);
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

                consoleCloseHandler += new EventHandler(ConsoleCloseHandler);
                SetConsoleCtrlHandler(consoleCloseHandler, true);
                
                // Set up all verbose event handlers.
                if (options.Debug || options.Verbose)
                {
                    Server.OnServicePacketSent += Server_OnServicePacketSent;
                    Server.OnServicePacketReceived += Server_OnServicePacketReceived;
                }
                
                try
                {
                    // Start the server.
                    await Server.Start();
                }
                catch (System.Net.HttpListenerException ex)
                {
                    Log.Fatal($"Unable to start listener for connections: {ex.Message}");

                    if (ex.ErrorCode == 5)
                        Log.Information("The requested operation requires elevation (Run as administrator).\n\n"
                       + $"To run as a user, execute 'netsh http add urlacl url=http://*:{options.Port}/ user=Everyone' as Administrator");

                    throw new ApplicationException($"Server startup failed: {ex.Message}");
                }
            });
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

        private static void Server_OnServerStarted(Server server)
        {
            if (Options.EnableApi)
            {
                ApiServer = new ApiServer(server, new ApiSettings(apiKey: Options.ServerDBApiKey, advertise: Options.Advertise, centralApiKey:Options.CentralApiKey));
                if(Options.Advertise != null)
                    ApiServer.registerServiceOnCentralAPI(true);
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
        
        // Handler for the console close event
        private static bool ConsoleCloseHandler()
        {
            if(Options?.Advertise != null)
                ApiServer?.registerServiceOnCentralAPI(false);
            Console.WriteLine("Console is closing. Performing cleanup...");
            Server?.Stop();
            
            Thread.Sleep(1500);
            
            // Allow the program to exit
            return true;
        }
    }
}
