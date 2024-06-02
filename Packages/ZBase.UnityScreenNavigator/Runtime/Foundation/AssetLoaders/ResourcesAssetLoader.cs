using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    public sealed class ResourcesAssetLoader : IAssetLoader
    {
        private readonly Dictionary<AssetLoadHandleId, AssetLoadHandle> _controlIdToHandles = new();

        private uint _nextControlId;

        public AssetLoadHandle<T> Load<T>(string key) where T : Object
        {
            var controlId = _nextControlId++;
            var handle = new AssetLoadHandle<T>(controlId);
            _controlIdToHandles.Add(controlId, handle);

            var result = Resources.Load<T>(key);
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
            var controlId = _nextControlId++;
            var handle = new AssetLoadHandle<T>(controlId);
            _controlIdToHandles.Add(controlId, handle);

            var tcs = new UniTaskCompletionSource<T>();
            var req = Resources.LoadAsync<T>(key);

            req.completed += _ =>
            {
                var result = req.asset as T;
                handle.SetResult(result);

                var status = result ? AssetLoadStatus.Success : AssetLoadStatus.Failed;
                handle.SetStatus(status);

                if (result == false)
                {
                    var exception = new InvalidOperationException($"Requested asset（Key: {key}）was not found.");
                    handle.SetOperationException(exception);
                }

                tcs.TrySetResult(result);
            };

            handle.SetPercentCompleteFunc(() => req.progress);
            handle.SetTask(tcs.Task);
            return handle;
        }

        public void Release(AssetLoadHandleId handleId)
        {
            if (_controlIdToHandles.TryGetValue(handleId, out var handle) == false)
            {
                return;
            }

            _controlIdToHandles.Remove(handleId);
            handle.SetTypelessResult(null);

            /// Resources.UnloadUnusedAssets() is responsible for releasing
            /// assets loaded by Resources.Load(), so nothing is done here.
            /// Don't use Resources.UnloadAsset.
        }
    }
}