using EchoRelay.Core.Properties;
using EchoRelay.Core.Server.Storage.Types;
using EchoRelay.Core.Utils;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Nakama;
using Nakama.TinyJson;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using Nk = Nakama;
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
            var session = Storage.Session;

            var readObjectId = new Nk.StorageObjectId
            {
                Collection = _objectCollection,
                Key = _objectKey,
                UserId = session.UserId,
            };

            var result = await client.ReadStorageObjectsAsync(session, new Nk.StorageObjectId[] { readObjectId });

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

            var client = Storage.Client;
            var session = Storage.Session;

            var readObjectId = new Nk.StorageObjectId
            {
                Collection = _objectCollection,
                Key = _objectKey,
                UserId = session.UserId,
            };

            var result = await client.ReadStorageObjectsAsync(session, new Nk.StorageObjectId[] { readObjectId });
            var storageObject = result.Objects.First();

            _resource = JsonConvert.DeserializeObject<V>(storageObject.Value);

            return _resource;
        }
        protected override void SetInternal(V resource)
        {
            var task = Task.Run(async () => { await SetInternalAsync(resource); });
            task.Wait();
        }
        protected async Task SetInternalAsync(V resource)
        {
            // Update the cached resource
            _resource = resource;

            var client = Storage.Client;
            var session = Storage.Session;

            var writeObject = new WriteStorageObject
            {
                Collection = _objectCollection,
                Key = _objectKey,
                Value = JsonConvert.SerializeObject(_resource, Formatting.Indented, StreamIO.JsonSerializerSettings),
                PermissionRead = 1, // Only the server and owner can read
                PermissionWrite = 1, // The server and owner can write
            };

            await client.WriteStorageObjectsAsync(session, new[] { writeObject });

            //string resourceJson = JsonConvert.SerializeObject(_resource, Formatting.Indented, StreamIO.JsonSerializerSettings);

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

            var result = await Storage.Client.ListUsersStorageObjectsAsync(Storage.Session, _collection, Storage.Session.UserId, 100);

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
            var session = Storage.Session;

            var readObjectId = new Nk.StorageObjectId
            {
                Collection = _collection,
                Key = _keySelectorFunc(key),
                UserId = session.UserId,
            };

            var result = await client.ReadStorageObjectsAsync(session, new Nk.StorageObjectId[] { readObjectId });
            var storageObject = result.Objects.First();
            var resource = JsonConvert.DeserializeObject<V>(storageObject.Value);

            if (resource == null)
            {
                return default;
            }
            _resources[key] = (_keySelectorFunc(key), resource);

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

            _resources[key] = (_keySelectorFunc(key), resource);

            var client = Storage.Client;
            var session = Storage.Session;

            var writeObject = new WriteStorageObject
            {
                Collection = _collection,
                Key = _keySelectorFunc(key),
                Value = JsonConvert.SerializeObject(resource, Formatting.Indented, StreamIO.JsonSerializerSettings),
                PermissionRead = 1, // Only the server and owner can read
                PermissionWrite = 1, // The server and owner can write
            };

            await client.WriteStorageObjectsAsync(session, new[] { writeObject });

            //string resourceJson = JsonConvert.SerializeObject(_resource, Formatting.Indented, StreamIO.JsonSerializerSettings);

        }

        protected override V? DeleteInternal(K key)
        {
            _resources.Remove(key, out var removed);
            return removed.Resource;
        }
    }

    /// <summary>
    /// A Nakama <see cref="ResourceCollectionProvider{K, V}"/> which maps EchoRelay.App accounts onto Nakama accounts.
    /// </summary>
    /// <typeparam name="K">The type of key which is used to index the resource.</typeparam>
    /// <typeparam name="V">The type of resources which should be managed by this provider.</typeparam>
    internal class NakamaAccountResourceProvider<K, V> : ResourceCollectionProvider<K, V>
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

        private object _lookupsChangeLock = new object();

        public NakamaAccountResourceProvider(NakamaServerStorage storage, string collection, Func<K, string> keySelectorFunc) : base(storage)
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

            var result = await Storage.Client.ListUsersStorageObjectsAsync(Storage.Session, _collection, Storage.Session.UserId, 100);

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
            // Use the key to get the user data

            var client = Storage.Client;
            var session = Storage.Session;

            var readObjectId = new Nk.StorageObjectId
            {
                Collection = _collection,
                Key = _keySelectorFunc(key),
                UserId = session.UserId,
            };

            var result = await client.ReadStorageObjectsAsync(session, new Nk.StorageObjectId[] { readObjectId });
            var resource = default(V);

            if (result.Objects.Any())
            {
                resource = JsonConvert.DeserializeObject<V>(result.Objects.First().Value);
                _resources[key] = (_keySelectorFunc(key), resource);
                return resource;
            } else
            {
                return default;
            }
        }
        protected override void SetInternal(K key, V resource)
        {
            _resources[key] = (_keySelectorFunc(key), resource);
            var task = Task.Run(async () => { await SetInternalAsync(key, resource); });
            task.Wait();

        }
        protected async Task SetInternalAsync(K key, V resource)
        {

            _resources[key] = (_keySelectorFunc(key), resource);

            var client = Storage.Client;
            var session = Storage.Session;

            var writeObject = new WriteStorageObject
            {
                Collection = _collection,
                Key = _keySelectorFunc(key),
                Value = JsonConvert.SerializeObject(resource, Formatting.Indented, StreamIO.JsonSerializerSettings),
                PermissionRead = 1, // Only the server and owner can read
                PermissionWrite = 1, // The server and owner can write
            };

            await client.WriteStorageObjectsAsync(session, new[] { writeObject });

            //string resourceJson = JsonConvert.SerializeObject(_resource, Formatting.Indented, StreamIO.JsonSerializerSettings);

        }

        protected override V? DeleteInternal(K key)
        {
            _resources.Remove(key, out var removed);
            return removed.Resource;
        }
    }


}
