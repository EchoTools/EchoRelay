using EchoRelay.Core.Server.Storage.Resources;
using EchoRelay.Core.Server.Storage.Types;
using EchoRelay.Core.Utils;
using Nakama;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;

namespace EchoRelay.Core.Server.Storage.Nakama
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
            return (GetInternal() != null);
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
                { typeof(AccessControlListResource), "echorelay/getAccessControlList" },
                { typeof(ChannelInfoResource), "echorelay/getChannelInfo" },
                { typeof(LoginSettingsResource), "echorelay/getLoginSettings" },
                { typeof(Resources.SymbolCache), null }
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
                await client.RpcAsync(session, "echorelay/setAccessControlList",
                    payload: JsonConvert.SerializeObject(accessControlListResource, StreamIO.JsonSerializerSettings));
                break;
            case ChannelInfoResource channelResource:
                await client.RpcAsync(session, "echorelay/setChannelInfo",
                    payload: JsonConvert.SerializeObject(channelResource, StreamIO.JsonSerializerSettings));
                break;
            case LoginSettingsResource loginSettingsResource:
                await client.RpcAsync(session, "echorelay/setLoginSetting",
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
    private readonly string _collection;

    private ConcurrentDictionary<K, (string key, V Resource)> _resources;

    public new NakamaServerStorage Storage { get; }

    public NakamaResourceCollectionProvider(NakamaServerStorage storage, string collection, Func<K, string> keySelectorFunc) : base(storage)
    {
        Storage = storage;
        _collection = collection;
        _resources = new ConcurrentDictionary<K, (string key, V)>();
        _keySelectorFunc = keySelectorFunc;
    }

    protected override void OpenInternal()
    {
        var task = Task.Run(async () => { await OpenInternalAsync(); });
        task.Wait();
    }

    public async Task OpenInternalAsync()
    {
        var session = await Storage.RefreshSessionAsync();
        var result = await Storage.Client.ListUsersStorageObjectsAsync(session, _collection, session.UserId, 100);

        foreach (IApiStorageObject configObject in result.Objects)
        {
            try
            {
                // Load the config resource.
                V? resource = JsonConvert.DeserializeObject<V>(configObject.Value);

                // Obtain the key for the resource
                K key = resource.Key();

                // Add it to our lookups
                _resources[key] = (configObject.Collection, resource);
            }
            catch (Exception ex)
            {
                Close();
                throw new Exception($"Could not load resource {typeof(V).Name}: '{configObject.Key}'", ex);
            }
        }
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
        return _resources.ContainsKey(key);
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
        V? resource = default;

        // do some reflection trickery
        switch (typeof(V))
        {
            case Type type when type == typeof(AccountResource):
                try
                {
                    // authenticate to the users account, if it exists
                    ISession userSession = await Storage.Client.AuthenticateDeviceAsync(id: objectId, create: false);
                    IApiRpc data = await Storage.Client.RpcAsync(userSession, "echorelay/getAccount");
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
                                BannedUntil = System.DateTime.MaxValue
                            }, typeof(V));
                        case 404: // Account not found
                        default:
                            return default;
                    }
                }
            case Type type when type == typeof(ConfigResource):
                try
                {
                    IApiRpc data = await Storage.Client.RpcAsync(session, "echorelay/getConfig", payload: $"{{\"id\":\"{objectId}\"}}");
                    if (data.Payload == null)
                        return default;
                    return JsonConvert.DeserializeObject<V>(data.Payload);
                }
                catch (ApiResponseException)
                {
                    return default;
                }
            default:
                // all other
                var readObjectId = new StorageObjectId
                {
                    Collection = _collection,
                    Key = _keySelectorFunc(key),
                    UserId = session.UserId,
                };
                resource = JsonConvert.DeserializeObject<V>((await client.ReadStorageObjectsAsync(session, new StorageObjectId[] { readObjectId })).Objects.First().Value);
                break;
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
            case AccountResource accountResource:
                var deviceId = resourceId;
                ISession userSession = await client.AuthenticateDeviceAsync(id: deviceId, username: accountResource.Profile.Client.DisplayName, create: true);
                await client.RpcAsync(userSession, "echorelay/setAccount",
                    payload: JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings));
                break;

            case ConfigResource configResource:
                await client.RpcAsync(session, "echorelay/setConfig",
                    payload: JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings));
                break;

            case DocumentResource documentResource:
                await client.RpcAsync(session, "echorelay/setDocument",
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
