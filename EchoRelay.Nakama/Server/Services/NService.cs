using EchoRelay.Nakama;


namespace EchoRelay.Core.Server.Services
{
    public abstract class NService : Service
    {
        protected NService(Server server, string name, EchoNakama nk) : base(server, name)
        {
            // Create the relay's nakama session
            Nk = nk;
        }

        public EchoNakama Nk { get; }
    }
}
