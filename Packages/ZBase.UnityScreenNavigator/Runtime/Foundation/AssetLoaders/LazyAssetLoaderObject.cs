using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    [CreateAssetMenu(fileName = "LazyAssetLoader", menuName = "Screen Navigator/Loaders/Lazy Asset Loader")]
    public sealed class LazyAssetLoaderObject : AssetLoaderObject, IAssetLoader, IInitializable, IDeinitializable
    {
        [SerializeField] private List<KeyAssetPair> _lazyObjects = new();

        private readonly LazyAssetLoader _loader = new();

        public List<KeyAssetPair> LazyObjects => _lazyObjects;

        public void Initialize()
        {
            var src = _lazyObjects;
            var dest = _loader.lazyObjects;
            dest.Clear();

            var count = src.Count;

            for (var i = 0; i < count; i++)
            {
                var preloadedObject = src[i];
                var key = preloadedObject.Key;

                if (string.IsNullOrEmpty(key))
                {
                    ErrorIfKeyIsNull(i, this);
                    continue;
                }

                var asset = preloadedObject.Asset;

                if (asset.instanceID == default)
                {
                    ErrorIfAssetIsNull(i, this);
                    continue;
                }

                if (dest.TryAdd(key, asset) == false)
                {
                    ErrorIfDuplicate(i, key, this);
                }
            }
        }

        public void Deinitialize()
        {
            _loader.lazyObjects.Clear();
            _loader.loadedObjects.Clear();
        }

        public override AssetLoadHandle<T> Load<T>(string key)
        {
            return _loader.Load<T>(key);
        }

        public override AssetLoadHandle<T> LoadAsync<T>(string key)
        {
            return _loader.LoadAsync<T>(key);
        }

        public override void Release(AssetLoadHandleId handleId)
        {
            _loader.Release(handleId);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfKeyIsNull(int index, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Key at {index} is null or empty", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfAssetIsNull(int index, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Asset at {index} is null", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfDuplicate(int index, string key, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Asset at index {index} cannot be registered because the key `{key}` is already existing", context);
        }

        [Serializable]
        public struct KeyAssetPair
        {
            [SerializeField] private KeySourceType _keySource;
            [SerializeField] private string _key;
            [SerializeField] private LazyLoadReference<Object> _asset;

            public KeySourceType KeySource
            {
                readonly get => _keySource;
                set => _keySource = value;
            }

            public string Key
            {
                readonly get => _key;
                set => _key = value;
            }

            public LazyLoadReference<Object> Asset
            {
                readonly get => _asset;
                set => _asset = value;
            }
        }
    }
}