using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.Collections;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class ModalContainer : ContainerLayer
    {
        private static Dictionary<int, ModalContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, ModalContainer> s_instanceCacheByName = new();

        private readonly List<IModalContainerCallbackReceiver> _callbackReceivers = new();
        private readonly List<ViewRef<Modal>> _modals = new();
        private readonly List<ViewRef<ModalBackdrop>> _backdrops = new();

        private bool _disableBackdrop;

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        /// <summary>
        /// Stacked modals.
        /// </summary>
        public IReadOnlyList<ViewRef<Modal>> Modals => _modals;

        /// <summary>
        /// Stacked backdrops.
        /// </summary>
        public IReadOnlyList<ViewRef<ModalBackdrop>> Backdrops => _backdrops;

        public ViewRef<Modal> Current => _modals[^1];

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        protected override void OnInitialize()
        {
            _callbackReceivers.AddRange(GetComponents<IModalContainerCallbackReceiver>());
            _disableBackdrop = Settings.DisableModalBackdrop;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var modals = _modals;
            var modalCount = modals.Count;

            for (var i = 0; i < modalCount; i++)
            {
                var (modal, resourcePath) = modals[i];
                DestroyAndForget(new ViewRef(modal, resourcePath, PoolingPolicy.DisablePooling)).Forget();
            }

            modals.Clear();

            var backdrops = _backdrops;
            var backdropCount = backdrops.Count;

            for (var i = 0; i < backdropCount; i++)
            {
                var (backdrop, resourcePath) = backdrops[i];
                DestroyAndForget(new ViewRef(backdrop, resourcePath, PoolingPolicy.DisablePooling)).Forget();
            }

            backdrops.Clear();

            s_instanceCacheByName.Remove(LayerName);

            using var keysToRemove = new PooledList<int>(s_instanceCacheByTransform.Count);

            foreach (var cache in s_instanceCacheByTransform)
            {
                if (Equals(cache.Value))
                {
                    keysToRemove.Add(cache.Key);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                s_instanceCacheByTransform.Remove(keyToRemove);
            }
        }

        private string GetBackdropResourcePath(string resourcePath)
        {
            return string.IsNullOrWhiteSpace(resourcePath)
                ? Settings.ModalBackdropResourcePath
                : resourcePath;
        }

        /// <summary>
        /// Get the <see cref="ModalContainer" /> that manages the modal to which <see cref="transform" /> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <see cref="transform" />.</param>
        /// <returns></returns>
        public static ModalContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="ModalContainer" /> that manages the modal to which <paramref name="rectTransform"/> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="rectTransform"/>.</param>
        /// <returns></returns>
        public static ModalContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();

            if (useCache && s_instanceCacheByTransform.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<ModalContainer>();

            if (container)
            {
                s_instanceCacheByTransform.Add(id, container);
                return container;
            }

            Debug.LogError($"Cannot find any parent {nameof(ModalContainer)} component", rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ModalContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static ModalContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            Debug.LogError($"Cannot find any {nameof(ModalContainer)} by name `{containerName}`");
            return null;
        }

        /// <summary>
        /// Find the <see cref="ModalContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out ModalContainer container)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            Debug.LogError($"Cannot find any {nameof(ModalContainer)} by name `{containerName}`");
            container = default;
            return false;
        }

        [Obsolete("This method is deprecated. Use Create(ContainerLayerConfig, IContainerLayerManager) instead")]
        public static async UniTask<ModalContainer> CreateAsync(
              ContainerLayerConfig layerConfig
            , IContainerLayerManager manager
            , UnityScreenNavigatorSettings settings
        )
        {
            var container = Create(layerConfig, manager, settings);
            await UniTask.NextFrame();
            return container;
        }

        /// <summary>
        /// Create a new <see cref="ModalContainer" /> as a layer.
        /// </summary>
        public static ModalContainer Create(
              ContainerLayerConfig layerConfig
            , IContainerLayerManager manager
            , UnityScreenNavigatorSettings settings
        )
        {
            var root = new GameObject(
                  layerConfig.name
                , typeof(Canvas)
                , typeof(GraphicRaycaster)
                , typeof(CanvasGroup)
            );

            var rectTransform = root.GetOrAddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localPosition = Vector3.zero;

            var container = root.AddComponent<ModalContainer>();
            container.Initialize(layerConfig, manager, settings);

            s_instanceCacheByName.Add(container.LayerName, container);
            return container;
        }

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(IModalContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(IModalContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Searches through the <see cref="Modals"/> stack
        /// and returns the index of the Modal loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="index">
        /// Return a value greater or equal to 0 if there is
        /// a Modal loaded from this <paramref name="resourcePath"/>.
        /// </param>
        /// <returns>
        /// True if there is a Modal loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public bool FindIndexOfRecentlyPushed(string resourcePath, out int index)
        {
            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var modals = _modals;

            for (var i = modals.Count - 1; i >= 0; i--)
            {
                if (string.Equals(resourcePath, modals[i].ResourcePath))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Searches through the <see cref="Modals"/> stack
        /// and destroys the Modal loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="ignoreFront">Do not destroy if the modal is in the front.</param>
        /// <returns>
        /// True if there is a Modal loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public void DestroyRecentlyPushed(string resourcePath, bool ignoreFront = true)
        {
            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var frontIndex = _modals.Count - 1;

            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            if (ignoreFront && frontIndex == index)
            {
                return;
            }

            var modal = _modals[index];
            _modals.RemoveAt(index);

            ViewRef<ModalBackdrop>? backdrop = null;

            if (_disableBackdrop == false)
            {
                backdrop = _backdrops[index];
                _backdrops.RemoveAt(index);
            }

            DestroyAndForget(modal).Forget();

            if (backdrop.HasValue)
            {
                DestroyAndForget(backdrop.Value).Forget();
            }
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Fire-and-forget</remarks>
        public void BringToFront(ModalOptions options, bool ignoreFront, params Arg[] args)
        {
            BringToFrontAndForget(options, ignoreFront, args).Forget();
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Asynchronous</remarks>
        public async UniTask BringToFrontAsync(ModalOptions options, bool ignoreFront, params Arg[] args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        private async UniTaskVoid BringToFrontAndForget(ModalOptions options, bool ignoreFront, Memory<Arg> args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        private async UniTask BringToFrontAsyncInternal(ModalOptions options, bool ignoreFront, Memory<Arg> args)
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var frontIndex = _modals.Count - 1;

            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            if (ignoreFront && frontIndex == index)
            {
                return;
            }

            var enterModal = _modals[index].View;
            enterModal.Settings = Settings;

            var modalId = enterModal.GetInstanceID();
            _modals.RemoveAt(index);
            RectTransform.RemoveChild(enterModal.transform);

            ViewRef<ModalBackdrop>? backdrop = null;

            if (_disableBackdrop == false)
            {
                var backdropAtIndex = _backdrops[index];
                _backdrops.RemoveAt(index);

                var backdropView = backdropAtIndex.View;
                RectTransform.RemoveChild(backdropView.transform);

                backdropView.Setup(RectTransform, options.backdropAlpha, options.closeWhenClickOnBackdrop);
                backdropView.Settings = Settings;

                _backdrops.Add(backdropAtIndex);
                backdrop = backdropAtIndex;
            }

            options.options.onLoaded?.Invoke(enterModal, args);

            await enterModal.AfterLoadAsync(RectTransform, args);

            var exitModal = _modals.Count == 0 ? null : _modals[^1].View;

            if (exitModal)
            {
                exitModal.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterModal, exitModal, args);
            }

            if (exitModal)
            {
                await exitModal.BeforeExitAsync(true, args);
            }

            await enterModal.BeforeEnterAsync(true, args);

            // Play Animation

            if (backdrop.HasValue && backdrop.Value.View)
            {
                await backdrop.Value.View.EnterAsync(options.options.playAnimation);
            }

            if (exitModal)
            {
                await exitModal.ExitAsync(true, options.options.playAnimation, enterModal);
            }

            await enterModal.EnterAsync(true, options.options.playAnimation, exitModal);

            // End Transition
            _modals.Add(new ViewRef<Modal>(enterModal, resourcePath, options.options.poolingPolicy));
            IsInTransition = false;

            // Postprocess
            if (exitModal)
            {
                exitModal.AfterExit(true, args);
            }

            enterModal.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterModal, exitModal, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Push<TModal>(ModalOptions options, params Arg[] args)
            where TModal : Modal
        {
            PushAndForget<TModal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Push(ModalOptions options, params Arg[] args)
        {
            PushAndForget<Modal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PushAsync<TModal>(ModalOptions options, params Arg[] args)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PushAsync(ModalOptions options, params Arg[] args)
        {
            await PushAsyncInternal<Modal>(options, args);
        }

        private async UniTaskVoid PushAndForget<TModal>(ModalOptions options, Memory<Arg> args)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        private async UniTask PushAsyncInternal<TModal>(ModalOptions options, Memory<Arg> args)
            where TModal : Modal
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            if (IsInTransition)
            {
                Debug.LogWarning("Cannot transition because there is a modal already in transition.");
                return;
            }

            IsInTransition = true;
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            ModalBackdrop backdrop = null;

            if (_disableBackdrop == false)
            {
                var backdropResourcePath = GetBackdropResourcePath(options.modalBackdropResourcePath);
                var backdropOptions = new WindowOptions(
                      resourcePath: backdropResourcePath
                    , playAnimation: options.options.playAnimation
                    , loadAsync: options.options.loadAsync
                    , poolingPolicy: PoolingPolicy.UseSettings
                );

                backdrop = await GetViewAsync<ModalBackdrop>(backdropOptions);
                backdrop.Setup(RectTransform, options.backdropAlpha, options.closeWhenClickOnBackdrop);
                _backdrops.Add(new ViewRef<ModalBackdrop>(backdrop, backdropResourcePath, backdropOptions.poolingPolicy));
            }

            var enterModal = await GetViewAsync<TModal>(options.options);
            options.options.onLoaded?.Invoke(enterModal, args);

            await enterModal.AfterLoadAsync(RectTransform, args);

            var exitModal = _modals.Count == 0 ? null : _modals[^1].View;

            if (exitModal)
            {
                exitModal.Settings = Settings;
            }    

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterModal, exitModal, args);
            }

            if (exitModal)
            {
                await exitModal.BeforeExitAsync(true, args);
            }

            await enterModal.BeforeEnterAsync(true, args);

            // Play Animation

            if (backdrop)
            {
                await backdrop.EnterAsync(options.options.playAnimation);
            }

            if (exitModal)
            {
                await exitModal.ExitAsync(true, options.options.playAnimation, enterModal);
            }

            await enterModal.EnterAsync(true, options.options.playAnimation, exitModal);

            // End Transition
            _modals.Add(new ViewRef<Modal>(enterModal, resourcePath, options.options.poolingPolicy));
            IsInTransition = false;

            // Postprocess
            if (exitModal)
            {
                exitModal.AfterExit(true, args);
            }

            enterModal.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterModal, exitModal, args);
            }
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Pop(bool playAnimation, params Arg[] args)
        {
            PopAndForget(playAnimation, args).Forget();
        }

        private async UniTaskVoid PopAndForget(bool playAnimation, params Arg[] args)
        {
            await PopAsync(playAnimation, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PopAsync(bool playAnimation, params Arg[] args)
        {
            if (_modals.Count == 0)
            {
                Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
                return;
            }

            if (IsInTransition)
            {
                Debug.LogWarning("Cannot transition because there is a modal already in transition.");
                return;
            }

            IsInTransition = true;
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var lastModalIndex = _modals.Count - 1;
            var exitModalRef = _modals[lastModalIndex];
            var exitModal = exitModalRef.View;
            exitModal.Settings = Settings;

            var enterModal = _modals.Count == 1 ? null : _modals[^2].View;

            if (enterModal)
            {
                enterModal.Settings = Settings;
            }

            ViewRef<ModalBackdrop>? backdrop = null;

            if (_disableBackdrop == false)
            {
                var lastBackdropIndex = _backdrops.Count - 1;
                var lastBackdrop = _backdrops[lastBackdropIndex];
                _backdrops.RemoveAt(lastBackdropIndex);

                lastBackdrop.View.Settings = Settings;
                backdrop = lastBackdrop;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePop(enterModal, exitModal, args);
            }

            await exitModal.BeforeExitAsync(false, args);

            if (enterModal != null)
            {
                await enterModal.BeforeEnterAsync(false, args);
            }

            // Play Animation
            await exitModal.ExitAsync(false, playAnimation, enterModal);

            if (enterModal != null)
            {
                await enterModal.EnterAsync(false, playAnimation, exitModal);
            }

            if (backdrop.HasValue && backdrop.Value.View)
            {
                await backdrop.Value.View.ExitAsync(playAnimation);
            }

            // End Transition
            _modals.RemoveAt(lastModalIndex);
            IsInTransition = false;

            // Postprocess
            exitModal.AfterExit(false, args);

            if (enterModal != null)
            {
                enterModal.AfterEnter(false, args);
            }

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPop(enterModal, exitModal, args);
            }

            // Unload unused Modal
            await exitModal.BeforeReleaseAsync();

            DestroyAndForget(exitModalRef).Forget();

            if (backdrop.HasValue)
            {
                DestroyAndForget(backdrop.Value).Forget();
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }
    }
}