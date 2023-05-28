using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public abstract class ContainerLayer : Window, IContainerLayer
    {
        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _resourcePathToHandle = new();
        private readonly Dictionary<string, Queue<View>> _resourcePathToPool = new();

        private IAssetLoader _assetLoader;

        public string LayerName { get; private set; }

        public ContainerLayerType LayerType { get; private set; }

        public IContainerLayerManager ContainerLayerManager { get; private set; }

        public Canvas Canvas { get; private set; }

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

        protected ContainerLayerConfig Config { get; private set; }

        protected RectTransform PoolTransform { get; private set; }

        protected override void OnDestroy()
        {
            foreach (var (resourcePath, pool) in _resourcePathToPool)
            {
                while (pool.TryDequeue(out var view))
                {
                    DestroyAndForget(new ViewRef(view, resourcePath, PoolingPolicy.DisablePooling)).Forget();
                }
            }

            _resourcePathToPool.Clear();
        }

        public void Initialize(
              ContainerLayerConfig config
            , IContainerLayerManager manager
            , UnityScreenNavigatorSettings settings
        )
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Settings = settings ? settings : throw new ArgumentNullException(nameof(settings));

            ContainerLayerManager = manager ?? throw new ArgumentNullException(nameof(manager));
            ContainerLayerManager.Add(this);

            LayerName = config.name;
            LayerType = config.layerType;
            
            var canvas = GetComponent<Canvas>();

            if (config.overrideSorting)
            {
                canvas.overrideSorting = true;
                canvas.sortingLayerID = config.sortingLayer.id;
                canvas.sortingOrder = config.orderInLayer;
            }

            Canvas = canvas;

            InitializePool(canvas);
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        private void InitializePool(Canvas canvas)
        {
            var parentTransform = this.transform.parent.GetComponent<RectTransform>();

            var poolGO = new GameObject(
                $"[Pool] {this.name}"
                , typeof(Canvas)
                , typeof(CanvasGroup)
            );

            PoolTransform = poolGO.GetOrAddComponent<RectTransform>();
            PoolTransform.SetParent(parentTransform, false);
            PoolTransform.FillParent(parentTransform);

            var poolCanvas = poolGO.GetComponent<Canvas>();
            poolCanvas.overrideSorting = true;
            poolCanvas.sortingLayerID = canvas.sortingLayerID;
            poolCanvas.sortingOrder = canvas.sortingOrder;

            var poolCanvasGroup = poolGO.GetComponent<CanvasGroup>();
            poolCanvasGroup.alpha = 0f;
            poolCanvasGroup.blocksRaycasts = false;
            poolCanvasGroup.interactable = false;
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
                Debug.LogWarning($"The amount of preloaded view instances should be greater than 0.");
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

            for (var i = 0; i < amount; i++)
            {
                InstantiateToPool(resourcePath, assetLoadHandle, pool);
            }
        }

        private void InstantiateToPool(
              string resourcePath
            , AssetLoadHandle<GameObject> assetLoadHandle
            , Queue<View> pool
        )
        {
            var instance = Instantiate(assetLoadHandle.Result);

            if (instance.TryGetComponent<View>(out var view) == false)
            {
                Debug.LogError(
                    $"Cannot find the {typeof(View).Name} component on the specified resource `{resourcePath}`."
                    , instance
                );

                return;
            }

            view.Settings = Settings;
            view.RectTransform.SetParent(PoolTransform);
            view.Parent = PoolTransform;
            view.Owner.SetActive(false);

            pool.Enqueue(view);
        }

        [Obsolete("This method is deprecated. Use ContainsInPool(string) instead.")]
        public bool IsPreloadRequested(string resourcePath)
            => ContainsInPool(resourcePath);

        [Obsolete("This method is deprecated. Use ContainsInPool(string) instead.")]
        public bool IsPreloaded(string resourcePath)
            => ContainsInPool(resourcePath);

        [Obsolete("This method is deprecated. Use KeepInPool(string, int) instead.")]
        public void ReleasePreloaded(string resourcePath)
            => KeepInPool(resourcePath, 0);

        protected async UniTask<T> GetViewAsync<T>(WindowOptions options)
            where T : View
        {
            var resourcePath = options.resourcePath;

            if (GetFromPool<T>(resourcePath, options.poolingPolicy, out var existView))
            {
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
                assetLoadHandle = options.loadAsync
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
                Debug.LogError(
                    $"Cannot find the {typeof(T).Name} component on the specified resource `{resourcePath}`."
                    , instance
                );

                return null;
            }

            view.Settings = Settings;

            if (handleInMap == false)
            {
                _resourcePathToHandle[resourcePath] = assetLoadHandle;
            }

            return view;
        }

        protected async UniTaskVoid DestroyAndForget(ViewRef viewRef)
        {
            if (ReturnToPool(viewRef))
            {
                return;
            }

            var view = viewRef.View;

            if (view && view.gameObject)
            {
                Destroy(view.gameObject);
                await UniTask.NextFrame();
            }

            if (ContainsInPool(viewRef.ResourcePath))
            {
                return;
            }

            if (_resourcePathToHandle.TryGetValue(viewRef.ResourcePath, out var handle))
            {
                AssetLoader.Release(handle.Id);
                _resourcePathToHandle.Remove(viewRef.ResourcePath);
            }
        }

        private bool GetFromPool<T>(string resourcePath, PoolingPolicy poolingPolicy, out T view)
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

        private bool ReturnToPool(ViewRef viewRef)
        {
            if (CanPool(viewRef.PoolingPolicy) == false)
            {
                return false;
            }

            var resourcePathToPool = _resourcePathToPool;

            if (resourcePathToPool.TryGetValue(viewRef.ResourcePath, out var pool) == false)
            {
                resourcePathToPool[viewRef.ResourcePath] = pool = new Queue<View>();
            }

            var view = viewRef.View;

            if (view.Owner == false)
            {
                return false;
            }

            view.RectTransform.SetParent(PoolTransform);
            view.Parent = PoolTransform;
            view.Owner.SetActive(false);
            pool.Enqueue(view);
            return true;
        }

        private bool CanPool(PoolingPolicy poolingPolicy)
        {
            if (poolingPolicy == PoolingPolicy.DisablePooling)
                return false;

            if (poolingPolicy == PoolingPolicy.EnablePooling)
                return true;

            return EnablePooling;
        }
    }
}