#if USN_USE_ADDRESSABLES

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    public sealed class AddressableAssetLoader : IAssetLoader
    {
        private readonly Dictionary<AssetLoadHandleId, AsyncOperationHandle> _controlIdToHandles = new();

        private uint _nextControlId;

        public bool SuppressErrorLogOnRelease { get; set; }

        public AssetLoadHandle<T> Load<T>(string key)
            where T : Object
        {
#if ADDRESSABLES_1_17_4_OR_NEWER
            var addressableHandle = Addressables.LoadAssetAsync<T>(key);
            var result = addressableHandle.IsDone
                ? addressableHandle.Result
                : addressableHandle.WaitForCompletion();

            var controlId = _nextControlId++;
            _controlIdToHandles.Add(controlId, addressableHandle);

            var handle = new AssetLoadHandle<T>(controlId);
            handle.SetPercentCompleteFunc(() => addressableHandle.PercentComplete);
            handle.SetTask(UniTask.FromResult(result));
            handle.SetResult(result);

            var status = addressableHandle.Status == AsyncOperationStatus.Succeeded
                ? AssetLoadStatus.Success
                : AssetLoadStatus.Failed;

            handle.SetStatus(status);
            handle.SetOperationException(addressableHandle.OperationException);

            return handle;
#else
            throw new System.NotSupportedException();
#endif
        }

        public AssetLoadHandle<T> LoadAsync<T>(string key)
            where T : Object
        {
            var addressableHandle = Addressables.LoadAssetAsync<T>(key);

            var controlId = _nextControlId++;
            _controlIdToHandles.Add(controlId, addressableHandle);

            var handle = new AssetLoadHandle<T>(controlId);
            var tcs = new UniTaskCompletionSource<T>();
            addressableHandle.Completed += x =>
            {
                handle.SetResult(x.Result);

                var status = x.Status == AsyncOperationStatus.Succeeded
                    ? AssetLoadStatus.Success
                    : AssetLoadStatus.Failed;

                handle.SetStatus(status);
                handle.SetOperationException(addressableHandle.OperationException);
                tcs.TrySetResult(x.Result);
            };

            handle.SetPercentCompleteFunc(() => addressableHandle.PercentComplete);
            handle.SetTask(tcs.Task);

            return handle;
        }

        public void Release(AssetLoadHandleId handleId)
        {
            if (_controlIdToHandles.TryGetValue(handleId, out var handle) == false)
            {
                if (SuppressErrorLogOnRelease == false)
                {
                    ErrorIfFoundNoAsset(handleId);
                }

                return;
            }

            _controlIdToHandles.Remove(handleId);
            Addressables.Release(handle);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfFoundNoAsset(AssetLoadHandleId handleId)
        {
            UnityEngine.Debug.LogError($"There is no asset that has been requested for release (Handle.Id: {handleId}).");
        }
    }
}

#endif