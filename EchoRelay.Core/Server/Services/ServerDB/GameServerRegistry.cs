﻿using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Messages.ServerDB;
using EchoRelay.Core.Utils;
using System.Collections.Concurrent;
using static EchoRelay.Core.Server.Messages.ServerDB.ERGameServerStartSession;

namespace EchoRelay.Core.Server.Services.ServerDB
{
    public class GameServerRegistry
    {
        #region Properties
        public static readonly Guid ZeroGuid = new Guid("00000000-0000-0000-0000-000000000000");
        public ConcurrentDictionary<ulong, RegisteredGameServer> RegisteredGameServers { get; }
        public ConcurrentDictionary<Guid, RegisteredGameServer> RegisteredGameServersBySessionId { get; }
        #endregion

        #region Events
        /// <summary>
        /// Event of a game server being registered/unregistered with the central server.
        /// </summary>
        /// <param name="gameServer">The <see cref="RegisteredGameServer"/> which was registered/unregistered with the central server.</param>
        public delegate void GameServerRegistrationChangedEventHandler(RegisteredGameServer gameServer);
        /// <summary>
        /// Event of a game server being registered.
        /// </summary>
        public event GameServerRegistrationChangedEventHandler? OnGameServerRegistered;
        /// <summary>
        /// Event of a game server being unregistered.
        /// </summary>
        public event GameServerRegistrationChangedEventHandler? OnGameServerUnregistered;
        #endregion

        #region Constructor
        public GameServerRegistry()
        {
            RegisteredGameServers = new ConcurrentDictionary<ulong, RegisteredGameServer>();
            RegisteredGameServersBySessionId = new ConcurrentDictionary<Guid, RegisteredGameServer>();
        }
        #endregion

        #region Functions
        public RegisteredGameServer AddGameServer(RegisteredGameServer registeredGameServer)
        {
            // Add the game server to our lookup
            RegisteredGameServers[registeredGameServer.ServerId] = registeredGameServer;

            // Fire the relevant event for the game server being registered.
            OnGameServerRegistered?.Invoke(registeredGameServer);
            return registeredGameServer;
        }

        public RegisteredGameServer? GetGameServer(ulong serverId)
        {
            RegisteredGameServers.TryGetValue(serverId, out RegisteredGameServer? registeredGameServer);
            return registeredGameServer;
        }

        public RegisteredGameServer? GetGameServer(Guid sessionId)
        {
            RegisteredGameServersBySessionId.TryGetValue(sessionId, out RegisteredGameServer? registeredGameServer);
            return registeredGameServer;
        }

        public void RemoveGameServer(RegisteredGameServer registeredGameServer)
        {
            RemoveGameServer(registeredGameServer.ServerId);
        }
        public void RemoveGameServer(ulong serverId)
        {
            // Try to remove any registered game server with this server identifier.
            RegisteredGameServers.Remove(serverId, out var unregisteredGameServer);

            // Remove stale session
            if (unregisteredGameServer != null && unregisteredGameServer.SessionStarted)
                RegisteredGameServersBySessionId.Remove((Guid)unregisteredGameServer.SessionId, out var _);

            // Fire the relevant event for the game server being registered.
            if (unregisteredGameServer != null)
                OnGameServerUnregistered?.Invoke(unregisteredGameServer);
        }

        public IEnumerable<RegisteredGameServer> FilterGameServers(
            int? findMax = null,
            ulong? serverId = null,
            Guid? sessionId = null,
            HashSet<(uint InternalAddr, uint ExternalAddr)>? addresses = null,
            ushort? port = null,
            long? gameTypeSymbol = null,
            long? levelSymbol = null,
            Guid? channel = null,
            bool? locked = null,
            LobbyType[]? lobbyTypes = null,
            TeamIndex? requestedTeam = null,
            bool unfilledServerOnly = true,
            TimeSpan? maxSessionAge = null,
            long? regionSymbol = null)
        {
            // Filter through all game servers
            List<RegisteredGameServer> filteredGameServers = new List<RegisteredGameServer>();
            foreach(RegisteredGameServer gameServer in RegisteredGameServers.Values)
            {
                // If we hit any set limit for game servers found, stop.
                if (findMax != null && filteredGameServers.Count >= findMax)
                    break;

                // Filter for each field supplied, skip to the next game server if any filter doesn't match.
                if (serverId != null && gameServer.ServerId != serverId)
                    continue;
                else if (addresses != null && !addresses.Contains((gameServer.InternalAddress.ToUInt32(), gameServer.ExternalAddress.ToUInt32())))
                    continue;
                else if (port != null && gameServer.Peer.Port != port)
                    continue;
                
                // If Region is supplied filter by RegionSybmol.
                if(regionSymbol != null && gameServer.RegionSymbol != regionSymbol)
                    continue;

                // If the session is started, filter on that criteria.
                if (gameServer.SessionStarted)
                {
                    if (gameTypeSymbol != null && gameTypeSymbol != gameServer.SessionGameTypeSymbol)
                        continue;
                    else if (levelSymbol != null && levelSymbol != gameServer.SessionLevelSymbol)
                        continue;
                    else if (channel != null && channel != ZeroGuid && gameServer.SessionChannel != ZeroGuid && channel != gameServer.SessionChannel) // zero guid means it is not a social lobby, etc, so we do not filter.
                        continue;
                    else if (locked != null && locked != gameServer.SessionLocked)
                        continue;
                    else if (lobbyTypes != null && !lobbyTypes.Contains(gameServer.SessionLobbyType))
                        continue;
                    else if (unfilledServerOnly && gameServer.SessionPlayerCount >= gameServer.SessionPlayerLimits.TotalPlayerLimit)
                        continue;
                    else if (unfilledServerOnly && requestedTeam != null && !gameServer.CheckTeamAvailability(requestedTeam.Value))
                        continue;
                    else if (maxSessionAge.HasValue && gameServer.SessionAge >= maxSessionAge.Value)
                        continue;
                }
                // Only add the server if it has a non-zero player count, or if the list does not already contain a server with the same external address.
                if (gameServer.SessionPlayerCount != 0 || !filteredGameServers.Any(gs => gs.ExternalAddress == gameServer.ExternalAddress))
                {
                    filteredGameServers.Add(gameServer);
                }
            }
            // Randomize the filtered game servers so that the population sort is not always the same.
            Random random = new Random();
            filteredGameServers = filteredGameServers.OrderBy(x => random.Next()).ToList();

            return filteredGameServers;
        }
        #endregion
    }
}
