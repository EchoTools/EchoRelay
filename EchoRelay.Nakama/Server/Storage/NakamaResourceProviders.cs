using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Services.Login;
using EchoRelay.Core.Server.Storage.Types;
using EchoRelay.Core.Utils;
using Nakama;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace EchoRelay.Core.Server.Storage
{
    /// <summary>
    /// A Nakama <see cref="ResourceProvider{V}"/> which storages a singular resource.
    /// </summary>
    /// <typeparam name="K">The type of key which is used to index the resource.</typeparam>
    /// <typeparam name="V">The type of resources which should be managed by this provider.</typeparam>
    internal class NakamaResourceProvider<V> : ResourceProvider<V>
    {
        /// <summary>
        /// A mapped resource to the Nakama API
        /// </summary>
        private V? _resource;

        public new NakamaServerStorage Storage { get; }

        private readonly string _objectCollection;
        private readonly string _objectKey;

        public NakamaResourceProvider(NakamaServerStorage storage, string objectCollection, string objectKey) : base(storage)
        {
            Storage = storage;

            _objectCollection = objectCollection;
            _objectKey = objectKey;
        }

        protected override void OpenInternal()
        {

        }

        protected override void CloseInternal()
        {

        }

        public override bool Exists()
        {
            return GetInternal() != null;
        }

        protected override V? GetInternal()
        {
            var task = Task.Run(async () => { return await GetInternalAsync(); });
            task.Wait();
            return task.Result;
        }

        protected async Task<V?> GetInternalAsync()
        {
            // do some reflection trickery
            var rpcMethodMap = new Dictionary<Type, string?>
            {
                { typeof(AccessControlListResource), "echorelay/getaccesscontrollist" },
                { typeof(ChannelInfoResource), "echorelay/getchannelinfo" },
                { typeof(LoginSettingsResource), "echorelay/getloginsettings" },
                { typeof(Core.Server.Storage.Resources.SymbolCache), null }
            };

            if (rpcMethodMap.TryGetValue(typeof(V), out var rpcMethod))
            {
                if (rpcMethod == null) // This an in-memory only resource
                {
                    return _resource;
                }
                try
                {
                    var client = Storage.Client;
                    var session = await Storage.RefreshSessionAsync();
                    IApiRpc data = await client.RpcAsync(session, rpcMethod, payload: $"{{\"id\":\"{_objectKey}\"}}");
                    if (data.Payload != null)
                    {
                        _resource = JsonConvert.DeserializeObject<V>(data.Payload);
                    }
                }
                catch (ApiResponseException ex)
                {
                    switch (ex.StatusCode)
                    {
                        case 403: // Banned Account
                            throw new Exception("Account is banned");
                        case 404: // Account not found
                            return default;
                    }
                }
            }
            return _resource;
        }

        protected override void SetInternal(V resource)
        {
            var task = Task.Run(async () => { await SetInternalAsync(resource); });
            task.Wait();
        }

        protected async Task SetInternalAsync(V resource)
        {
            var client = Storage.Client;
            var session = await Storage.RefreshSessionAsync();
            _resource = resource;

            switch (resource)
            {
                case AccessControlListResource accessControlListResource:
                    await client.RpcAsync(session, "echorelay/setaccesscontrollist",
                        payload: JsonConvert.SerializeObject(accessControlListResource, StreamIO.JsonSerializerSettings));
                    break;
                case ChannelInfoResource channelResource:
                    await client.RpcAsync(session, "echorelay/setchannelinfo",
                        payload: JsonConvert.SerializeObject(channelResource, StreamIO.JsonSerializerSettings));
                    break;
                case LoginSettingsResource loginSettingsResource:
                    await client.RpcAsync(session, "echorelay/setloginsettings",
                        payload: JsonConvert.SerializeObject(loginSettingsResource, StreamIO.JsonSerializerSettings));
                    break;
            }
        }
        protected override V? DeleteInternal()
        {
            // Store a reference to our cached resource
            V? resource = _resource;

            // Clear the cached resource.
            _resource = default;

            // Return the removed resource, if any.
            return resource;
        }
    }

    /// <summary>
    /// A Nakama <see cref="ResourceCollectionProvider{K, V}"/> which storages a given type of keyed resource in a collection.
    /// </summary>
    /// <typeparam name="K">The type of key which is used to index the resource.</typeparam>
    /// <typeparam name="V">The type of resources which should be managed by this provider.</typeparam>
    internal class NakamaResourceCollectionProvider<K, V> : ResourceCollectionProvider<K, V>
        where K : notnull
        where V : IKeyedResource<K>
    {
        /// <summary>
        /// The directory containing the resources.
        /// </summary>

        private Func<K, string> _keySelectorFunc;
        private Func<K, string> _typeSelectorFunc;

        private ConcurrentDictionary<K, (string key, V Resource)> _resources;

        public new NakamaServerStorage Storage { get; }

        public NakamaResourceCollectionProvider(NakamaServerStorage storage, Func<K, string> collectionSelectorFunc, Func<K, string> keySelectorFunc) : base(storage)
        {
            Storage = storage;
            _typeSelectorFunc = collectionSelectorFunc;
            _keySelectorFunc = keySelectorFunc;
            _resources = new ConcurrentDictionary<K, (string key, V)>();
        }

        protected override void OpenInternal()
        {

        }

        protected override void CloseInternal()
        {
            _resources.Clear();
        }

        public override K[] Keys()
        {
            return _resources.Keys.ToArray();
        }

        public override bool Exists(K key)
        {
            return GetInternal(key) != null;
        }

        protected override V? GetInternal(K key)
        {
            var task = Task.Run(async () => { return await GetInternalAsync(key); });
            task.Wait();
            return task.Result;
        }

        protected async Task<V?> GetInternalAsync(K key)
        {
            var client = Storage.Client;
            var session = await Storage.RefreshSessionAsync();
            var objectId = _keySelectorFunc(key);
            var objectType = _typeSelectorFunc(key);

            V? resource = default;

            // do some reflection trickery
            switch (typeof(V))
            {
                case Type type when type == typeof(AccountResource):
                    try
                    {
                        // authenticate to the users account, if it exists
                        var deviceId = NLoginService.GetDeviceId(objectId);
                        if (String.IsNullOrEmpty(deviceId))
                            return default;
                        ISession userSession = await Storage.Client.AuthenticateDeviceAsync(deviceId, create: false);
                        IApiRpc data = await Storage.Client.RpcAsync(userSession, "echorelay/getaccount");
                        if (data.Payload == null)
                            return default;

                        return JsonConvert.DeserializeObject<V>(data.Payload);
                    }
                    catch (ApiResponseException ex)
                    {
                        switch (ex.StatusCode)
                        {
                            case 403: // Banned Account
                                      // If the user is banned, the server will only return 503. This
                                      // Return a fake resource with the ban in place.
                                return (V)Convert.ChangeType(new AccountResource
                                {
                                    BannedUntil = DateTime.MaxValue
                                }, typeof(V));
                            case 404: // Account not found
                            default:
                                return default;
                        }
                    }
                case Type type when type == typeof(ConfigResource):
                    try
                    {
                        IApiRpc data = await Storage.Client.RpcAsync(session, "echorelay/getconfig", payload: $"{{\"type\":\"{objectType}\",\"id\":\"{objectId}\"}}");
                        if (data.Payload == null)
                            return default;
                        return JsonConvert.DeserializeObject<V>(data.Payload);
                    }
                    catch (ApiResponseException)
                    {
                        return default;
                    }
                case Type type when type == typeof(DocumentResource):
                    try
                    {
                        IApiRpc data = await Storage.Client.RpcAsync(session, "echorelay/getdocument", payload: $"{{\"type\":\"{objectType}\",\"id\":\"{objectId}\"}}");
                        if (data.Payload == null)
                            return default;
                        return JsonConvert.DeserializeObject<V>(data.Payload);
                    }
                    catch (ApiResponseException)
                    {
                        return default;
                    }
            }
            return resource;
        }

        protected override void SetInternal(K key, V resource)
        {
            _resources[key] = (_keySelectorFunc(key), resource);
            var task = Task.Run(async () => { await SetInternalAsync(key, resource); });
            task.Wait();
        }

        protected async Task SetInternalAsync(K key, V resource)
        {
            var client = Storage.Client;
            var session = await Storage.RefreshSessionAsync();
            var resourceId = _keySelectorFunc(key);
            _resources[key] = (resourceId, resource);

            switch (resource)
            {
                case AccountResource:
                    var deviceId = NLoginService.GetDeviceId(resourceId);
                    if (String.IsNullOrEmpty(deviceId))
                        throw new Exception("Invalid device id");
                    ISession userSession = await client.AuthenticateDeviceAsync(deviceId, create: false);
                    await client.RpcAsync(userSession, "echorelay/setaccount",
                        payload: JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings));
                    break;

                case ConfigResource:
                    await client.RpcAsync(session, "echorelay/setconfig",
                        payload: JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings));
                    break;

                case DocumentResource:
                    await client.RpcAsync(session, "echorelay/setdocument",
                        payload: JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings));
                    break;

                default:
                    break;
            }
        }

        protected override V? DeleteInternal(K key)
        {
            _resources.Remove(key, out var removed);
            return removed.Resource;
        }
    }
}
