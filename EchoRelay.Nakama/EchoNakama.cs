using Nakama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoRelay.Nakama
{
    public class EchoNakama
    {
        private readonly Uri _nakamaUri;
        private readonly string _serverKey;

        public Client? Client { get; private set; }
        public ISession? Session { get; private set; }
        public string? RelayId { get; private set; }

        public async Task<ISession> RefreshSessionAsync()
        {
            Client ??= Connect();
            if (Session is null || Session.IsExpired)
                Session = await Client.AuthenticateDeviceAsync(RelayId, username: RelayId, create: true);
            else
                Session = await Client.SessionRefreshAsync(Session);
            return Session;
        }

        public Client Connect()
        {
            Client = new Client(_nakamaUri.Scheme, _nakamaUri.Host, _nakamaUri.Port, _serverKey);
            return Client;
        }

        public EchoNakama(Uri nakamaUri, string serverKey, string relayId)
        {
            _nakamaUri = nakamaUri; 
            _serverKey = serverKey;
            RelayId = relayId; 
        }
    }
}
