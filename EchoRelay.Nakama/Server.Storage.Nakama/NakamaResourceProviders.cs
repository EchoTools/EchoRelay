using EchoRelay.Core.Properties;
using EchoRelay.Core.Server.Storage.Types;
using EchoRelay.Core.Utils;
using Microsoft.AspNetCore.Routing;
using Nakama;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

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
            var task = Task.Run(async () => { return await ExistsAsync(); });
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Ensure the ACL Allow/Deny groups exist.
        /// </summary>
        public async Task<bool> ExistsAsync()
        {
            var client = Storage.Client;
            var session = await Storage.RefreshSessionAsync();

            var readObjectId = new StorageObjectId
            {
                Collection = _objectCollection,
                Key = _objectKey,
                UserId = session.UserId,
            };

            var result = await client.ReadStorageObjectsAsync(session, new StorageObjectId[] { readObjectId });

            if (result.Objects.Any())
            {
                var storageObject = result.Objects.First();
                _resource = JsonConvert.DeserializeObject<V>(storageObject.Value);
                return true;
            }
            return false;
        }
        enum GroupUserState : int
        {
            SuperAdmin = 0,
            Admin = 1,
            Member = 2,
            JoinRequest = 3
        }
        protected override V? GetInternal()
        {
            var task = Task.Run(async () => { return await GetInternalAsync(); });
            task.Wait();
            return task.Result;
        }


        protected async Task<V?> GetInternalAsync()
        {
            V? _resource = default(V);

            // do some reflection trickery
            switch (typeof(V))
            {
                default:
                    var client = Storage.Client;
                    var session = await Storage.RefreshSessionAsync();

                    var readObjectId = new StorageObjectId
                    {
                        Collection = _objectCollection,
                        Key = _objectKey,
                        UserId = session.UserId,
                    };

                    var result = await client.ReadStorageObjectsAsync(session, new StorageObjectId[] { readObjectId });
                    var storageObject = result.Objects.First();
                    _resource = JsonConvert.DeserializeObject<V>(storageObject.Value);

                    if (_resource == null)
                    {
                        return default;
                    }
                    break;
            }
            return _resource;
        }

        protected override void SetInternal(V resource)
        {
            var task = Task.Run(async () => { await SetInternalAsync(resource); });
            task.Wait();
        }

        /*
        protected async Task SetChannelInfoResourceAsync(ChannelInfoResource resource) {
            var session = await Storage.RefreshSessionAsync();
            
            foreach (var channel in resource.Group) {
                try
                {
                    await Storage.Client.JoinGroupAsync(session, channel.ChannelUUID);
                     
                } catch (ApiResponseException ex) {
                    var group = await Storage.Client.CreateGroupAsync(session, $"channel-{channel.Name}", channel.Description, maxCount: 100000);
                    channel.ChannelUUID = group.Id;
                }
            }
        }
        */
        protected async Task SetGenericResourceAsync(V resource)
        {
            var session = await Storage.RefreshSessionAsync();

            var writeObject = new WriteStorageObject
            {
                Collection = _objectCollection,
                Key = _objectKey,
                Value = JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings),
                PermissionRead = 1, // Only the server and owner can read
                PermissionWrite = 1, // The server and owner can write
            };

            await Storage.Client.WriteStorageObjectsAsync(session, new[] { writeObject });
        }
        protected async Task SetInternalAsync(V resource)
        {
            _resource = resource;

            switch (resource)
            {
                default:
                    await SetGenericResourceAsync(resource);
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
        private string ModeratorGroupName = "moderator";

        private ConcurrentDictionary<K, (string key, V Resource)> _resources;

        public new NakamaServerStorage Storage { get; }

        private object _lookupsChangeLock = new object();

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
            V? resource = default(V);

            // do some reflection trickery
            switch (typeof(V))
            {
                case Type type when type == typeof(AccountResource):

                    AccountResource accountResource = await GetAccountResourceAsync(key);
                    return (V)Convert.ChangeType(accountResource, typeof(V));

                default:
                    var client = Storage.Client;
                    var session = await Storage.RefreshSessionAsync();

                    var readObjectId = new StorageObjectId
                    {
                        Collection = _collection,
                        Key = _keySelectorFunc(key),
                        UserId = session.UserId,
                    };

                    var result = await client.ReadStorageObjectsAsync(session, new StorageObjectId[] { readObjectId });
                    var storageObject = result.Objects.First();
                    resource = JsonConvert.DeserializeObject<V>(storageObject.Value);
                    break;
            }

            return resource;
        }

        protected async Task<AccountResource?> GetAccountResourceAsync(K key)
        {
            var resource = new AccountResource();
            var client = Storage.Client;
            var session = await Storage.RefreshSessionAsync();
            var userName = _keySelectorFunc(key);
            ISession? userSession;

            // authenticate to the users account for a session
            try
            {
                userSession = await Storage.Client.AuthenticateDeviceAsync(userName, create : false);
            }
            catch (ApiResponseException ex)
            {
                switch (ex.StatusCode)
                {
                    // Banned Account
                    case 403:
                        resource.BannedUntil = System.DateTime.MaxValue;
                        return resource;
                    // Account not found
                    case 404:
                        return null;
                    default:
                        return null;
                }
            }

            var userAccount = await Storage.Client.GetAccountAsync(userSession);

            // manually map the data onto the Nakama user

            resource.Profile.Client.DisplayName = userAccount.User.DisplayName;

            // Check for the moderator group membership
            resource.IsModerator = false;
            var result = await Storage.Client.ListGroupsAsync(session, ModeratorGroupName);
            if (result.Groups.Any())
            {
                var groupId = result.Groups.First().Id;
                // state 2 is a member
                var gResult = await client.ListGroupUsersAsync(session, groupId, state: 2, limit: 100, result.Cursor);
                while (result.Cursor != null)
                {
                    gResult = await client.ListGroupUsersAsync(session, groupId, state: 2, limit: 100, result.Cursor);
                    if (gResult.GroupUsers.Where(gU => gU.User.Id == userAccount.User.Id).Any())
                    {
                        resource.IsModerator = true;
                        break;
                    }
                }
            }
            else
            {
                // Create the group if it's missing
                await Storage.Client.CreateGroupAsync(session, ModeratorGroupName, maxCount : 1000);
                resource.IsModerator = false;
            }

            var storageObject = await client.ReadStorageObjectsAsync(userSession, new[] {
              new StorageObjectId{
                  Collection = "relayConfig",
                    Key = "authSecrets",
                    UserId = userSession.UserId
              },
                new StorageObjectId {
                Collection = "profile",
                Key = "client",
                UserId = userSession.UserId
              },
              new StorageObjectId {
                Collection = "profile",
                Key = "server",
                UserId = userSession.UserId
              }
            });

            foreach (var obj in storageObject.Objects)
            {
                switch ((obj.Collection, obj.Key))
                {
                    case ("relayConfig", "authSecrets"):
                        var data = JsonConvert.DeserializeObject<Dictionary<string, byte[]>>(obj.Value);
                        _nonPublicSet(resource, "AccountLockHash", data["AccountLockHash"]);
                        _nonPublicSet(resource, "AccountLockSalt", data["AccountLockSalt"]);
                        break;
                    case ("profile", "client"):
                        resource.Profile.Client = JsonConvert.DeserializeObject<AccountResource.AccountClientProfile>(obj.Value);
                        break;
                    case ("profile", "server"):
                        resource.Profile.Server = JsonConvert.DeserializeObject<AccountResource.AccountServerProfile>(obj.Value);
                        break;
                }
            }

            return resource;
        }
        private void _nonPublicSet(AccountResource instance, string propertyName, object newValue)
        {
            PropertyInfo propertyInfo = typeof(AccountResource).GetProperty(propertyName);
            MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
            setMethod.Invoke(instance, new object[] { newValue });
        }
        protected override void SetInternal(K key, V resource)
        {
            _resources[key] = (_keySelectorFunc(key), resource);
            var task = Task.Run(async () => { await SetInternalAsync(key, resource); });
            task.Wait();

        }
        protected async Task SetAccountResourceAsync(K key, AccountResource resource)
        {
            var client = Storage.Client;
            var session = await Storage.RefreshSessionAsync();

            var userId = _keySelectorFunc(key);
            // authenticate to the users account for a session
            ISession userSession;
            try
            {
                // If the custom id exists, link the deviceId to that session, and clear the custom
                userSession = await Storage.Client.AuthenticateCustomAsync(userId, create : false);
                await client.LinkDeviceAsync(userSession, userId);
                await client.UnlinkCustomAsync(userSession, userId);
            } catch (ApiResponseException ex)
            {
                userSession = await Storage.Client.AuthenticateDeviceAsync(userId, userId, create: true);
            } finally
            {
                userSession = await Storage.Client.AuthenticateDeviceAsync(userId, userId, create: false);
            }

            var userAccount = await Storage.Client.GetAccountAsync(userSession);
            // manually map the data onto the Nakama user

            await Storage.Client.UpdateAccountAsync(userSession, userId, displayName: resource.Profile.Client.DisplayName);


            // The only top level members we need are the account hash and salt
            Dictionary<string, byte[]> authSecrets = new Dictionary<string, byte[]>();
            authSecrets.Add("AccountLockHash", resource.AccountLockHash);
            authSecrets.Add("AccountLockSalt", resource.AccountLockSalt);

            var writeObjects = new[] {
                new WriteStorageObject {
                    Collection = "relayConfig",
                    Key = "authSecrets",
                    Value = JsonConvert.SerializeObject(authSecrets, StreamIO.JsonSerializerSettings),
                    PermissionRead = 1, // Only the server and owner can read
                    PermissionWrite = 1, // The server and owner can write
                },
                new WriteStorageObject
                {
                    Collection = "profile",
                    Key = "client",
                    Value = JsonConvert.SerializeObject(resource.Profile.Client, StreamIO.JsonSerializerSettings),
                                PermissionRead = 1, // Only the server and owner can read
                                PermissionWrite = 1, // The server and owner can write
                },
                new WriteStorageObject
                {
                    Collection = "profile",
                    Key = "server",
                    Value = JsonConvert.SerializeObject(resource.Profile.Server, StreamIO.JsonSerializerSettings),
                                PermissionRead = 1, // Only the server and owner can read
                                PermissionWrite = 1, // The server and owner can write
                }
            };

            await client.WriteStorageObjectsAsync(userSession, writeObjects);
        }
        protected async Task SetInternalAsync(K key, V resource)
        {
            var session = await Storage.RefreshSessionAsync();
            _resources[key] = (_keySelectorFunc(key), resource);

            switch (resource)
            {
                case AccountResource accountResource:
                    await SetAccountResourceAsync(key, accountResource);
                    break;

                default:
                    var writeObject = new WriteStorageObject
                    {
                        Collection = _collection,
                        Key = _keySelectorFunc(key),
                        Value = JsonConvert.SerializeObject(resource, StreamIO.JsonSerializerSettings),
                        PermissionRead = 1, // Only the server and owner can read
                        PermissionWrite = 1, // The server and owner can write
                    };

                    await Storage.Client.WriteStorageObjectsAsync(session, new[] { writeObject });
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
