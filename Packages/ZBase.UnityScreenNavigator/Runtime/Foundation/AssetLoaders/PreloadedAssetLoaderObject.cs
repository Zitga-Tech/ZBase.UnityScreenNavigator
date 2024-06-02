using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    [CreateAssetMenu(fileName = "PreloadedAssetLoader", menuName = "Screen Navigator/Loaders/Preloaded Asset Loader")]
    public sealed class PreloadedAssetLoaderObject : AssetLoaderObject, IAssetLoader, IInitializable, IDeinitializable
    {
        [SerializeField] private List<KeyAssetPair> _preloadedObjects = new();

        private readonly PreloadedAssetLoader _loader = new();

        public List<KeyAssetPair> PreloadedObjects => _preloadedObjects;

        public void Initialize()
        {
            var src = _preloadedObjects;
            var dest = _loader.preloadedObjects;
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

                if (dest.TryAdd(key, preloadedObject.Asset) == false)
                {
                    ErrorIfDuplicate(i, key, this);
                }
            }
        }

        public void Deinitialize()
        {
            _loader.preloadedObjects.Clear();
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
            UnityEngine.Debug.LogError($"Object key at {index} is null or empty", context);
        }
        
        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfDuplicate(int index, string key, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Object at index {index} cannot be registered because the key `{key}` is already existing", context);
        }

        [Serializable]
        public sealed class KeyAssetPair
        {
            public enum KeySourceType
            {
                InputField,
                AssetName
            }

            [SerializeField] private KeySourceType _keySource = KeySourceType.AssetName;
            [SerializeField] private string _key;
            [SerializeField] private Object _asset;

            public KeySourceType KeySource
            {
                get => _keySource;
                set => _keySource = value;
            }

            public string Key
            {
                get => GetKey();
                set => _key = value;
            }

            public Object Asset
            {
                get => _asset;
                set => _asset = value;
            }

            private string GetKey()
            {
                if (_keySource == KeySourceType.AssetName)
                    return _asset == false ? "" : _asset.name;
                return _key;
            }
        }
    }
}