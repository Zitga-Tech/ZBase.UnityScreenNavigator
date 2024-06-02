using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    /// <summary>
    /// <see cref="IAssetLoader" /> that allows you to register Lazy assets.
    /// </summary>
    public sealed class LazyAssetLoader : IAssetLoader
    {
        private uint _nextControlId;
        internal readonly Dictionary<string, LazyLoadReference<Object>> lazyObjects = new();
        internal readonly Dictionary<string, Object> loadedObjects = new();

        public IReadOnlyDictionary<string, Object> LoadedObjects => loadedObjects;

        public AssetLoadHandle<T> Load<T>(string key) where T : Object
        {
            ErrorIfNoObject(lazyObjects.Count + loadedObjects.Count);

            var controlId = _nextControlId++;

            var handle = new AssetLoadHandle<T>(controlId);
            T result = null;

            if (loadedObjects.TryGetValue(key, out var obj))
            {
                result = obj as T;
            }

            if (result == false && lazyObjects.TryGetValue(key, out var lazyObj))
            {
                result = lazyObj.asset as T;
            }

            handle.SetResult(result);

            var status = result ? AssetLoadStatus.Success : AssetLoadStatus.Failed;
            handle.SetStatus(status);

            if (result == false)
            {
                var exception = new InvalidOperationException($"Requested asset（Key: {key}）was not found.");
                handle.SetOperationException(exception);
            }

            handle.SetPercentCompleteFunc(() => 1.0f);
            handle.SetTask(UniTask.FromResult(result));
            return handle;
        }

        public AssetLoadHandle<T> LoadAsync<T>(string key) where T : Object
        {
            return Load<T>(key);
        }

        /// <summary>
        /// This class does not release any objects.
        /// </summary>
        /// <param name="handleId"></param>
        public void Release(AssetLoadHandleId handleId)
        {
        }

        /// <summary>
        /// Register an object to <see cref="LoadedObjects" />. The asset name is used as the key.
        /// If you want to set your own key, add item to <see cref="LoadedObjects" /> directly.
        /// </summary>
        /// <param name="obj"></param>
        public void Register(Object obj)
        {
            if (obj == false)
            {
                ErrorIfObjectIsNull();
                return;
            }

            if (loadedObjects.TryAdd(obj.name, obj) == false)
            {
                ErrorIfDuplicate(obj.name, obj);
            }
        }

        public void Register(string key, Object obj)
        {
            if (obj == false)
            {
                ErrorIfObjectIsNull();
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                ErrorIfKeyIsNull();
                return;
            }    

            if (loadedObjects.TryAdd(key, obj) == false)
            {
                ErrorIfDuplicate(key, obj);
            }
        }
        
        public void Register(string key, LazyLoadReference<Object> obj)
        {
            if (obj.instanceID == default)
            {
                ErrorIfObjectIsNull();
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                ErrorIfKeyIsNull();
                return;
            }    

            if (lazyObjects.TryAdd(key, obj) == false)
            {
                ErrorIfDuplicate(key);
            }
        }

        public bool Contains(string key)
            => string.IsNullOrEmpty(key) == false
            && (loadedObjects.ContainsKey(key) || lazyObjects.ContainsKey(key));

        public bool Contains(Object obj)
            => obj == true && loadedObjects.ContainsKey(obj.name);

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfNoObject(int count)
        {
            if (count < 1)
            {
                UnityEngine.Debug.LogError(
                    "No object has been registered to this loader. " +
                    "Before using this loader, should invoke either UnityScreenNavigatorSettings.Initialize(), " +
                    "LazyAssetLoaderObject.Initialize(), or " +
                    "LazyAssetLoader.Register()."
                );
            }
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfDuplicate(string key, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Another object has been registered with key `{key}`", context);
        }
        
        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfDuplicate(string key)
        {
            UnityEngine.Debug.LogError($"Another object has been registered with key `{key}`");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfObjectIsNull()
        {
            UnityEngine.Debug.LogError("Object cannot be null");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfKeyIsNull()
        {
            UnityEngine.Debug.LogError("Key cannot be null or empty");
        }
    }
}