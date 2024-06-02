using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    /// <summary>
    /// <see cref="IAssetLoader" /> that allows you to register preloaded assets.
    /// </summary>
    public sealed class PreloadedAssetLoader : IAssetLoader
    {
        private uint _nextControlId;

        public Dictionary<string, Object> PreloadedObjects { get; } = new Dictionary<string, Object>();

        public AssetLoadHandle<T> Load<T>(string key) where T : Object
        {
            var controlId = _nextControlId++;

            var handle = new AssetLoadHandle<T>(controlId);
            T result = null;

            if (PreloadedObjects.TryGetValue(key, out var obj))
                result = obj as T;

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
        /// Add a object to <see cref="PreloadedObjects" />. The asset name is used as the key.
        /// If you want to set your own key, add item to <see cref="PreloadedObjects" /> directly.
        /// </summary>
        /// <param name="obj"></param>
        public void AddObject(Object obj)
        {
            PreloadedObjects.Add(obj.name, obj);
        }
    }
}