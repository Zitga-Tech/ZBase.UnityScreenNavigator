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
    /// <see cref="IAssetLoader" /> that allows you to register preloaded assets.
    /// </summary>
    public sealed class PreloadedAssetLoader : IAssetLoader
    {
        private uint _nextControlId;
        internal readonly Dictionary<string, Object> preloadedObjects = new();

        public IReadOnlyDictionary<string, Object> PreloadedObjects => preloadedObjects;

        public AssetLoadHandle<T> Load<T>(string key) where T : Object
        {
            ErrorIfNoObject(preloadedObjects);

            var controlId = _nextControlId++;

            var handle = new AssetLoadHandle<T>(controlId);
            T result = null;

            if (preloadedObjects.TryGetValue(key, out var obj))
            {
                result = obj as T;
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
        /// Register an object to <see cref="PreloadedObjects" />. The asset name is used as the key.
        /// If you want to set your own key, add item to <see cref="PreloadedObjects" /> directly.
        /// </summary>
        /// <param name="obj"></param>
        public void Register(Object obj)
        {
            if (obj == false)
            {
                ErrorIfObjectIsNull();
                return;
            }

            if (preloadedObjects.TryAdd(obj.name, obj) == false)
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

            if (preloadedObjects.TryAdd(key, obj) == false)
            {
                ErrorIfDuplicate(key, obj);
            }
        }

        public bool Contains(string key)
            => string.IsNullOrEmpty(key) == false && preloadedObjects.ContainsKey(key);

        public bool Contains(Object obj)
            => obj == true && preloadedObjects.ContainsKey(obj.name);

        [Obsolete("Use Register(Object) instead.", false)]
        public void AddObject(Object obj)
            => Register(obj);

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfNoObject(Dictionary<string, Object> dictionary)
        {
            if (dictionary == null || dictionary.Count < 1)
            {
                UnityEngine.Debug.LogError(
                    "No preloaded object has been registered to this loader. " +
                    "Before using this loader, should invoke either UnityScreenNavigatorSettings.Initialize(), " +
                    "PreloadedAssetLoaderObject.Initialize(), or " +
                    "PreloadedAssetLoader.Register()."
                );
            }
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfDuplicate(string key, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Another preloaded object has been registered with key `{key}`", context);
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