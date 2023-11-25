using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Storage.Resources;
using EchoRelay.Core.Server.Storage.Types;

namespace EchoRelay.Core.Server.Storage
{
    public interface IServerStorage
    {
        ResourceProvider<AccessControlListResource> AccessControlList { get; }
        ResourceCollectionProvider<XPlatformId, AccountResource> Accounts { get; }
        ResourceProvider<ChannelInfoResource> ChannelInfo { get; }
        ResourceCollectionProvider<(string type, string identifier), ConfigResource> Configs { get; }
        ResourceCollectionProvider<(string type, string language), DocumentResource> Documents { get; }
        ResourceProvider<LoginSettingsResource> LoginSettings { get; }
        bool Opened { get; }
        ResourceProvider<SymbolCache> SymbolCache { get; }

        event ServerStorage.StorageOpenedClosedEventHandler? OnStorageClosed;
        event ServerStorage.StorageOpenedClosedEventHandler? OnStorageOpened;

        void Clear(bool accessControlList = true, bool accounts = true, bool channelInfo = true, bool configs = true, bool documents = true, bool loginSettings = true, bool symbolCache = true);
        void Close();
        void Open();
    }
}