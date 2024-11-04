using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public abstract class ViewContainerBase : View, IViewContainer
    {
        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _resourcePathToHandle = new();
        private readonly Dictionary<string, Queue<View>> _resourcePathToPool = new();

        private IAssetLoader _assetLoader;

        /// <summary>
        /// By default, <see cref="IAssetLoader" /> in <see cref="UnityScreenNavigatorSettings" /> is used.
        /// If this property is set, it is used instead.
        /// </summary>
        public IAssetLoader AssetLoader
        {
            get => _assetLoader ?? Settings.AssetLoader;
            set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected bool EnablePooling
        {
            get => Settings.EnablePooling;
        }

        protected RectTransform PoolTransform { get; set; }

        protected virtual void InitializePool()
        {
            var parentTransform = this.transform.parent.GetComponent<RectTransform>();

            var poolGO = new GameObject(
                  $"[Pool] {this.name}"
                , typeof(CanvasGroup)
                , typeof(LayoutElement)
            );

            PoolTransform = poolGO.GetOrAddComponent<RectTransform>();
            PoolTransform.SetParent(parentTransform, false);
            PoolTransform.FillParent(parentTransform);

            var poolCanvasGroup = poolGO.GetComponent<CanvasGroup>();
            poolCanvasGroup.alpha = 0f;
            poolCanvasGroup.blocksRaycasts = false;
            poolCanvasGroup.interactable = false;

            var layoutElement = poolGO.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }

        protected override void OnDestroy()
        {
            foreach (var (resourcePath, pool) in _resourcePathToPool)
            {
                while (pool.TryDequeue(out var view))
                {
                    DestroyAndForget(view, resourcePath, PoolingPolicy.DisablePooling).Forget();
                }
            }

            _resourcePathToPool.Clear();
        }

        /// <summary>
        /// Returns the number of view instances currently in the pool
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public int CountInPool(string resourcePath)
            => _resourcePathToPool.TryGetValue(resourcePath, out var pool) ? pool.Count : 0;

        /// <summary>
        /// Returns true if there is at least one view instance in the pool.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public bool ContainsInPool(string resourcePath)
            => _resourcePathToPool.TryGetValue(resourcePath, out var pool) && pool.Count > 0;

        /// <summary>
        /// Only keep an amount of view instances in the pool,
        /// destroy other redundant instances.
        /// </summary>
        /// <param name="resourcePath">Resource path of the view</param>
        /// <param name="amount">The number of view instances to keep</param>
        /// <remarks>Fire-and-forget</remarks>
        public void KeepInPool(string resourcePath, int amount)
        {
            KeepInPoolAndForget(resourcePath, amount).Forget();
        }

        private async UniTaskVoid KeepInPoolAndForget(string resourcePath, int amount)
        {
            await KeepInPoolAsync(resourcePath, amount);
        }

        /// <summary>
        /// Only keep an amount of view instances in the pool,
        /// destroy other redundant instances.
        /// </summary>
        /// <param name="resourcePath">Resource path of the view</param>
        /// <param name="amount">The number of view instances to keep</param>
        /// <remarks>Asynchronous</remarks>
        public async UniTask KeepInPoolAsync(string resourcePath, int amount)
        {
            if (_resourcePathToPool.TryGetValue(resourcePath, out var pool) == false)
            {
                return;
            }

            var amountToDestroy = pool.Count - Mathf.Clamp(amount, 0, pool.Count);

            if (amountToDestroy < 1)
            {
                return;
            }

            var doDestroying = false;

            for (var i = 0; i < amountToDestroy; i++)
            {
                if (pool.TryDequeue(out var view))
                {
                    if (view && view.gameObject)
                    {
                        Destroy(view.gameObject);
                        doDestroying = true;
                    }
                }
            }

            if (doDestroying)
            {
                await UniTask.NextFrame();
            }

            if (pool.Count < 1
                && _resourcePathToHandle.TryGetValue(resourcePath, out var handle)
            )
            {
                AssetLoader.Release(handle.Id);
                _resourcePathToHandle.Remove(resourcePath);
            }
        }

        /// <summary>
        /// Preload an amount of view instances and keep them in the pool.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Preload(string resourcePath, bool loadAsync = true, int amount = 1)
        {
            PreloadAndForget(resourcePath, loadAsync, amount).Forget();
        }

        private async UniTaskVoid PreloadAndForget(string resourcePath, bool loadAsync = true, int amount = 1)
        {
            await PreloadAsync(resourcePath, loadAsync, amount);
        }

        /// <summary>
        /// Preload an amount of view instances and keep them in the pool.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PreloadAsync(string resourcePath, bool loadAsync = true, int amount = 1)
        {
            if (amount < 1)
            {
                WarningIfAmountLesserThanOne();
                return;
            }

            if (_resourcePathToPool.TryGetValue(resourcePath, out var pool) == false)
            {
                _resourcePathToPool[resourcePath] = pool = new Queue<View>();
            }

            if (pool.Count >= amount)
            {
                return;
            }

            var assetLoadHandle = loadAsync
                ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                : AssetLoader.Load<GameObject>(resourcePath);

            while (assetLoadHandle.IsDone == false)
            {
                await UniTask.NextFrame();
            }

            if (assetLoadHandle.Status == AssetLoadStatus.Failed)
            {
                throw assetLoadHandle.OperationException;
            }

            _resourcePathToHandle[resourcePath] = assetLoadHandle;

            var differentAmount = amount - pool.Count;

            for (var i = 0; i < differentAmount; i++)
            {
                InstantiateToPool(resourcePath, assetLoadHandle, pool);
            }
        }

        private void InstantiateToPool(string resourcePath, AssetLoadHandle<GameObject> assetLoadHandle, Queue<View> pool)
        {
            var instance = Instantiate(assetLoadHandle.Result);

            if (instance.TryGetComponent<View>(out var view) == false)
            {
                ErrorIfFoundNoComponent(resourcePath, instance);
                return;
            }

            view.Settings = Settings;
            view.RectTransform.SetParent(PoolTransform, false);
            view.Parent = PoolTransform;
            view.Owner.SetActive(false);

            pool.Enqueue(view);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected async UniTask<T> GetViewAsync<T>(ViewOptions options)
            where T : View
            => await GetViewAsync<T>(options.resourcePath, options.loadAsync, options.poolingPolicy);

        protected async UniTask<T> GetViewAsync<T>(string resourcePath, bool loadAsync, PoolingPolicy poolingPolicy)
            where T : View
        {
            if (GetFromPool<T>(resourcePath, poolingPolicy, out var existView))
            {
                existView.Settings = Settings;
                return existView;
            }

            AssetLoadHandle<GameObject> assetLoadHandle;
            var handleInMap = false;

            if (_resourcePathToHandle.TryGetValue(resourcePath, out var handle))
            {
                assetLoadHandle = handle;
                handleInMap = true;
            }
            else
            {
                assetLoadHandle = loadAsync
                    ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                    : AssetLoader.Load<GameObject>(resourcePath);
            }

            while (assetLoadHandle.IsDone == false)
            {
                await UniTask.NextFrame();
            }

            if (assetLoadHandle.Status == AssetLoadStatus.Failed)
            {
                throw assetLoadHandle.OperationException;
            }

            var instance = Instantiate(assetLoadHandle.Result);

            if (instance.TryGetComponent<T>(out var view) == false)
            {
                ErrorIfFoundNoComponent<T>(resourcePath, instance);
                return null;
            }

            view.Settings = Settings;

            if (handleInMap == false)
            {
                _resourcePathToHandle[resourcePath] = assetLoadHandle;
            }

            return view;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DestroyAndForget<T>(ViewRef<T> viewRef) where T : View
            => DestroyAndForget(viewRef.View, viewRef.ResourcePath, viewRef.PoolingPolicy).Forget();

        protected async UniTaskVoid DestroyAndForget<T>(T view, string resourcePath, PoolingPolicy poolingPolicy)
            where T : View
        {
            if (ReturnToPool(view, resourcePath, poolingPolicy))
            {
                return;
            }

            if (view && view.Owner)
            {
                Destroy(view.Owner);
                await UniTask.NextFrame();
            }

            if (ContainsInPool(resourcePath))
            {
                return;
            }

            if (_resourcePathToHandle.TryGetValue(resourcePath, out var handle))
            {
                AssetLoader.Release(handle.Id);
                _resourcePathToHandle.Remove(resourcePath);
            }
        }

        protected bool GetFromPool<T>(string resourcePath, PoolingPolicy poolingPolicy, out T view)
            where T : View
        {
            if (CanPool(poolingPolicy)
                && _resourcePathToPool.TryGetValue(resourcePath, out var pool)
                && pool.TryDequeue(out var typelessView)
            )
            {
                if (typelessView is T typedView)
                {
                    view = typedView;
                    view.Settings = Settings;
                    view.Owner.SetActive(true);
                    return true;
                }

                if (typelessView && typelessView.gameObject)
                {
                    Destroy(typelessView.Owner);
                }
            }

            view = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ReturnToPool<T>(ViewRef<T> viewRef)
            where T : View
            => ReturnToPool<T>(viewRef.View, viewRef.ResourcePath, viewRef.PoolingPolicy);

        protected bool ReturnToPool<T>(T view, string resourcePath, PoolingPolicy poolingPolicy)
            where T : View
        {
            if (view == false)
            {
                return false;
            }

            if (CanPool(poolingPolicy) == false)
            {
                return false;
            }

            if (_resourcePathToPool.TryGetValue(resourcePath, out var pool) == false)
            {
                _resourcePathToPool[resourcePath] = pool = new Queue<View>();
            }

            if (view.Owner == false)
            {
                return false;
            }

            view.RectTransform.SetParent(PoolTransform, false);
            view.Parent = PoolTransform;
            view.Owner.SetActive(false);
            pool.Enqueue(view);
            return true;
        }

        protected bool CanPool(PoolingPolicy poolingPolicy)
        {
            if (poolingPolicy == PoolingPolicy.DisablePooling)
                return false;

            if (poolingPolicy == PoolingPolicy.EnablePooling)
                return true;

            return EnablePooling;
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void WarningIfAmountLesserThanOne()
        {
            UnityEngine.Debug.LogWarning($"The amount of preloaded view instances should be greater than 0.");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfFoundNoComponent(string resourcePath, GameObject context)
        {
            UnityEngine.Debug.LogError(
                $"Cannot find any component derived from {typeof(View)} on the specified resource `{resourcePath}`."
                , context
            );
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfFoundNoComponent<T>(string resourcePath, GameObject context)
        {
            UnityEngine.Debug.LogError(
                $"Cannot find the {typeof(T)} component on the specified resource `{resourcePath}`."
                , context
            );
        }
    }
}
