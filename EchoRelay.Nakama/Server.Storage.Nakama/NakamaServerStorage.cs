using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Storage.Resources;
using EchoRelay.Core.Server.Storage.Types;
using Nakama;
using Nk = Nakama;
namespace EchoRelay.Core.Server.Storage.Nakama
{
    public class NakamaServerStorage : ServerStorage
    {
        /// <summary>
        /// The Nakama client to be used for storage.
        /// </summary>

        public override ResourceProvider<AccessControlListResource> AccessControlList => _accessControlList;
        private NakamaResourceProvider<AccessControlListResource> _accessControlList;

        public override ResourceCollectionProvider<XPlatformId, AccountResource> Accounts => _accounts;
        private NakamaResourceCollectionProvider<XPlatformId, AccountResource> _accounts;

        public override ResourceProvider<ChannelInfoResource> ChannelInfo => _channelInfo;
        private NakamaResourceProvider<ChannelInfoResource> _channelInfo;

        public override ResourceCollectionProvider<(string type, string identifier), ConfigResource> Configs => _configs;
        private NakamaResourceCollectionProvider<(string type, string identifier), ConfigResource> _configs;

        public override ResourceCollectionProvider<(string type, string language), DocumentResource> Documents => _documents;
        private NakamaResourceCollectionProvider<(string type, string language), DocumentResource> _documents;

        public override ResourceProvider<LoginSettingsResource> LoginSettings => _loginSettings;
        private NakamaResourceProvider<LoginSettingsResource> _loginSettings;

        public override ResourceProvider<SymbolCache> SymbolCache => _symbolCache;
        private ResourceProvider<SymbolCache> _symbolCache;

        public Nk.Client Client;
        private Session _session;

        public async Task<Nk.Session> RefreshSessionAsync()
        {
            if (_session == null || _session.IsExpired)
                return (Nk.Session)await Client.AuthenticateDeviceAsync(RelayId, create: true);
            else
                return (Nk.Session)await Client.SessionRefreshAsync(_session);
        }
        public string RelayId { get; }

        public NakamaServerStorage(Nk.Client client, Nk.Session session, string relayId)
        {
            Client = client;
            _session = session;
            RelayId = relayId;

            // Create our resource containers
            _accessControlList = new NakamaResourceProvider<AccessControlListResource>(this, "AccessControlListResource", "AccessControlList");
            _channelInfo = new NakamaResourceProvider<ChannelInfoResource>(this, "ChannelInfo", "channelInfo");
            _accounts = new NakamaResourceCollectionProvider<XPlatformId, AccountResource>(this, "Account",  x => $"{x}");
            _configs = new NakamaResourceCollectionProvider<(string Type, string Identifier), ConfigResource>(this, "Config", x => $"{x.Identifier}");
            _documents = new NakamaResourceCollectionProvider<(string Type, string Language), DocumentResource>(this, "Document", x => $"{x.Type}_{x.Language}");
            _loginSettings = new NakamaResourceProvider<LoginSettingsResource>(this, "LoginSettings", "loginSettings");
            _symbolCache = new NakamaResourceProvider<SymbolCache>(this, "SymbolCache", "symbolCache");
        }

        public static async Task<NakamaServerStorage> ConnectNakamaStorageAsync(string scheme, string host, int port, string serverKey, string deviceId)
        {
            var client = new Nk.Client(scheme, host, port, serverKey);
            var session = await client.AuthenticateDeviceAsync(deviceId, username : deviceId, create: true);
            return new NakamaServerStorage(client, (Nk.Session)session, deviceId);
        }
    }
}
