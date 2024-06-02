using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Core.Windows;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public class ModalContainer : WindowContainerBase
    {
        private static Dictionary<int, ModalContainer> s_instancesCachedByTransformId = new();
        private static Dictionary<string, ModalContainer> s_instancesCachedByName = new();

        public static IReadOnlyCollection<ModalContainer> Containers => s_instancesCachedByTransformId.Values;

#if UNITY_EDITOR
        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instancesCachedByTransformId = new();
            s_instancesCachedByName = new();
        }
#endif

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
                DestroyAndForget(modal, resourcePath, PoolingPolicy.DisablePooling).Forget();
            }

            modals.Clear();

            var backdrops = _backdrops;
            var backdropCount = backdrops.Count;

            for (var i = 0; i < backdropCount; i++)
            {
                var (backdrop, resourcePath) = backdrops[i];
                DestroyAndForget(backdrop, resourcePath, PoolingPolicy.DisablePooling).Forget();
            }

            backdrops.Clear();

            s_instancesCachedByName.Remove(LayerName);

            using var keysToRemove = new PooledList<int>(s_instancesCachedByTransformId.Count);

            foreach (var cache in s_instancesCachedByTransformId)
            {
                if (Equals(cache.Value))
                {
                    keysToRemove.Add(cache.Key);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                s_instancesCachedByTransformId.Remove(keyToRemove);
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

            if (useCache && s_instancesCachedByTransformId.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<ModalContainer>();

            if (container)
            {
                s_instancesCachedByTransformId.Add(id, container);
                return container;
            }

            ErrorCannotFindParent(rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ModalContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static ModalContainer Find(string containerName)
        {
            if (s_instancesCachedByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            ErrorIfCannotFindContainer(containerName);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ModalContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out ModalContainer container)
        {
            if (s_instancesCachedByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            ErrorIfCannotFindContainer(containerName);
            container = default;
            return false;
        }

        /// <summary>
        /// Create a new instance of <see cref="ModalContainer"/> as a layer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ModalContainer Create(
              WindowContainerConfig layerConfig
            , IWindowContainerManager manager
            , UnityScreenNavigatorSettings settings
        )
        {
            return Create<ModalContainer>(layerConfig, manager, settings);
        }

        /// <summary>
        /// Create a new instance of <typeparamref name="TContainer"/> as a layer.
        /// </summary>
        public static TContainer Create<TContainer>(
              WindowContainerConfig layerConfig
            , IWindowContainerManager manager
            , UnityScreenNavigatorSettings settings
        )
            where TContainer : ModalContainer
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

            var container = root.GetOrAddComponent<TContainer>();
            container.Initialize(layerConfig, manager, settings);

            s_instancesCachedByName.TryAdd(container.LayerName, container);
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

            DestroyAndForget(modal);

            if (backdrop.HasValue)
            {
                DestroyAndForget(backdrop.Value);
            }
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BringToFront(ModalOptions options, bool ignoreFront, params object[] args)
        {
            BringToFrontAndForget(options, ignoreFront, args).Forget();
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BringToFront(ModalOptions options, bool ignoreFront, Memory<object> args = default)
        {
            BringToFrontAndForget(options, ignoreFront, args).Forget();
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask BringToFrontAsync(ModalOptions options, bool ignoreFront, params object[] args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask BringToFrontAsync(ModalOptions options, bool ignoreFront, Memory<object> args = default)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid BringToFrontAndForget(ModalOptions options, bool ignoreFront, Memory<object> args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        private async UniTask BringToFrontAsyncInternal(ModalOptions options, bool ignoreFront, Memory<object> args)
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

            var animBackdrop = backdrop.HasValue && backdrop.Value.View
                ? backdrop.Value.View.EnterAsync(options.options.playAnimation)
                : default;

            var animExit = exitModal
                ? exitModal.ExitAsync(true, options.options.playAnimation, enterModal)
                : default;

            var animEnter = enterModal.EnterAsync(true, options.options.playAnimation, exitModal);

            await UniTask.WhenAll(animBackdrop, animExit, animEnter);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push<TModal>(ModalOptions options, params object[] args)
            where TModal : Modal
        {
            PushAndForget<TModal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push<TModal>(ModalOptions options, Memory<object> args = default)
            where TModal : Modal
        {
            PushAndForget<TModal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ModalOptions options, params object[] args)
        {
            PushAndForget<Modal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ModalOptions options, Memory<object> args = default)
        {
            PushAndForget<Modal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync<TModal>(ModalOptions options, params object[] args)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync<TModal>(ModalOptions options, Memory<object> args = default)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync(ModalOptions options, params object[] args)
        {
            await PushAsyncInternal<Modal>(options, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync(ModalOptions options, Memory<object> args = default)
        {
            await PushAsyncInternal<Modal>(options, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid PushAndForget<TModal>(ModalOptions options, Memory<object> args)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        private async UniTask PushAsyncInternal<TModal>(ModalOptions options, Memory<object> args)
            where TModal : Modal
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            if (IsInTransition)
            {
                ErrorIfCannotTransition();
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
                var backdropOptions = new ViewOptions(
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

            var animBackdrop = backdrop
                ? backdrop.EnterAsync(options.options.playAnimation)
                : default;

            var animExit = exitModal
                ? exitModal.ExitAsync(true, options.options.playAnimation, enterModal)
                : default;

            var animEnter = enterModal.EnterAsync(true, options.options.playAnimation, exitModal);

            await UniTask.WhenAll(animBackdrop, animExit, animEnter);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pop(bool playAnimation, params object[] args)
        {
            PopAndForget(playAnimation, args).Forget();
        }
        
        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pop(bool playAnimation, Memory<object> args = default)
        {
            PopAndForget(playAnimation, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PopAsync(bool playAnimation, params object[] args)
        {
            await PopAsyncInternal(playAnimation, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PopAsync(bool playAnimation, Memory<object> args = default)
        {
            await PopAsyncInternal(playAnimation, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid PopAndForget(bool playAnimation, Memory<object> args)
        {
            await PopAsyncInternal(playAnimation, args);
        }

        private async UniTask PopAsyncInternal(bool playAnimation, Memory<object> args)
        {
            if (_modals.Count == 0)
            {
                ErrorIfCannotTransitionBecauseNoModal();
                return;
            }

            if (IsInTransition)
            {
                ErrorIfCannotTransition();
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
            var animExit =  exitModal.ExitAsync(false, playAnimation, enterModal);

            var animEnter = enterModal != null
                ? enterModal.EnterAsync(false, playAnimation, exitModal)
                : default;

            var animBackdrop = backdrop.HasValue && backdrop.Value.View
                ? backdrop.Value.View.ExitAsync(playAnimation)
                : default;

            await UniTask.WhenAll(animExit, animEnter, animBackdrop);

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
            await exitModal.BeforeReleaseAsync(args);

            DestroyAndForget(exitModalRef);

            if (backdrop.HasValue)
            {
                DestroyAndForget(backdrop.Value);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorCannotFindParent(RectTransform rectTransform)
        {
            UnityEngine.Debug.LogError($"Cannot find any parent {nameof(ModalContainer)} component", rectTransform);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotFindContainer(string containerName)
        {
            UnityEngine.Debug.LogError($"Cannot find any {nameof(ModalContainer)} by name `{containerName}`");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotTransition()
        {
            UnityEngine.Debug.LogError("Cannot transition because there is a modal already in transition.");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotTransitionBecauseNoModal()
        {
            UnityEngine.Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
        }
    }
}