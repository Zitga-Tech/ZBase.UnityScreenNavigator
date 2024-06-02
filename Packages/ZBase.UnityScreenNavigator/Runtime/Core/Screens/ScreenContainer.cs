using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Windows;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public class ScreenContainer : WindowContainerBase
    {
        private static Dictionary<int, ScreenContainer> s_instancesCacheByTransformId = new();
        private static Dictionary<string, ScreenContainer> s_instancesCacheByName = new();

        public static IReadOnlyCollection<ScreenContainer> Containers => s_instancesCacheByTransformId.Values;

#if UNITY_EDITOR
        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instancesCacheByTransformId = new();
            s_instancesCacheByName = new();
        }
#endif

        private readonly List<IScreenContainerCallbackReceiver> _callbackReceivers = new();
        private readonly List<ViewRef<Screen>> _screens = new();

        private bool _isActiveScreenStacked;

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        /// <summary>
        /// Stacked screens.
        /// </summary>
        public IReadOnlyList<ViewRef<Screen>> Screens => _screens;

        public ViewRef<Screen> Current => _screens[^1];

        protected override void Awake()
        {
            _callbackReceivers.AddRange(GetComponents<IScreenContainerCallbackReceiver>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var screens = _screens;
            var count = screens.Count;

            for (var i = 0; i < count; i++)
            {
                var (screen, resourcePath) = screens[i];
                DestroyAndForget(screen, resourcePath, PoolingPolicy.DisablePooling).Forget();
            }

            screens.Clear();
            s_instancesCacheByName.Remove(LayerName);

            using var keysToRemove = new PooledList<int>(s_instancesCacheByTransformId.Count);

            foreach (var cache in s_instancesCacheByTransformId)
            {
                if (Equals(cache.Value))
                {
                    keysToRemove.Add(cache.Key);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                s_instancesCacheByTransformId.Remove(keyToRemove);
            }
        }

        #region STATIC_METHODS

        /// <summary>
        /// Get the <see cref="ScreenContainer" /> that manages the screen to which <paramref name="transform"/> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="transform"/>.</param>
        /// <returns></returns>
        public static ScreenContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="ScreenContainer" /> that manages the screen to which <paramref name="rectTransform"/> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="rectTransform"/>.</param>
        /// <returns></returns>
        public static ScreenContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();

            if (useCache && s_instancesCacheByTransformId.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<ScreenContainer>();

            if (container)
            {
                s_instancesCacheByTransformId.Add(id, container);
                return container;
            }

            ErrorCannotFindParent(rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ScreenContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static ScreenContainer Find(string containerName)
        {
            if (s_instancesCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            ErrorIfCannotFindContainer(containerName);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ScreenContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out ScreenContainer container)
        {
            if (s_instancesCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            ErrorIfCannotFindContainer(containerName);
            container = default;
            return false;
        }

        /// <summary>
        /// Create a new instance <see cref="ScreenContainer"/> as a layer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScreenContainer Create(
              WindowContainerConfig layerConfig
            , IWindowContainerManager manager
            , UnityScreenNavigatorSettings settings
        )
        {
            return Create<ScreenContainer>(layerConfig, manager, settings);
        }

        /// <summary>
        /// Create a new instance <typeparamref name="TContainer"/> as a layer.
        /// </summary>
        public static TContainer Create<TContainer>(
              WindowContainerConfig layerConfig
            , IWindowContainerManager manager
            , UnityScreenNavigatorSettings settings
        )
            where TContainer : ScreenContainer
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

            s_instancesCacheByName.TryAdd(container.LayerName, container);
            return container;
        }

        #endregion

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        public void AddCallbackReceiver(IScreenContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        public void RemoveCallbackReceiver(IScreenContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Searches through the <see cref="Screens"/> stack
        /// and returns the index of the Screen loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="index">
        /// Return a value greater or equal to 0 if there is
        /// a Screen loaded from this <paramref name="resourcePath"/>.
        /// </param>
        /// <returns>
        /// True if there is a Screen loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public bool FindIndexOfRecentlyPushed(string resourcePath, out int index)
        {
            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var screens = _screens;

            for (var i = screens.Count - 1; i >= 0; i--)
            {
                if (string.Equals(resourcePath, screens[i].ResourcePath))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Searches through the <see cref="Screens"/> stack
        /// and destroys the Screen loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="ignoreFront">Do not destroy if the screen is in the front.</param>
        /// <returns>
        /// True if there is a Screen loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public void DestroyRecentlyPushed(string resourcePath, bool ignoreFront = true)
        {
            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var frontIndex = _screens.Count - 1;

            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            if (ignoreFront && frontIndex == index)
            {
                return;
            }

            var screen = _screens[index];
            _screens.RemoveAt(index);

            DestroyAndForget(screen);
        }

        /// <summary>
        /// Bring an instance of <see cref="Screen"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the screen is already in the front.</param>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BringToFront(ScreenOptions options, bool ignoreFront, params object[] args)
        {
            BringToFrontAndForget(options, ignoreFront, args).Forget();
        }
        
        /// <summary>
        /// Bring an instance of <see cref="Screen"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the screen is already in the front.</param>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BringToFront(ScreenOptions options, bool ignoreFront, Memory<object> args = default)
        {
            BringToFrontAndForget(options, ignoreFront, args).Forget();
        }

        /// <summary>
        /// Bring an instance of <see cref="Screen"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the screen is already in the front.</param>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask BringToFrontAsync(ScreenOptions options, bool ignoreFront, params object[] args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }
        
        /// <summary>
        /// Bring an instance of <see cref="Screen"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the screen is already in the front.</param>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask BringToFrontAsync(ScreenOptions options, bool ignoreFront, Memory<object> args = default)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid BringToFrontAndForget(ScreenOptions options, bool ignoreFront, Memory<object> args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        private async UniTask BringToFrontAsyncInternal(ScreenOptions options, bool ignoreFront, Memory<object> args)
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var frontIndex = _screens.Count - 1;

            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            if (ignoreFront && frontIndex == index)
            {
                return;
            }

            var enterScreen = _screens[index].View;
            enterScreen.Settings = Settings;

            var screenId = enterScreen.GetInstanceID();
            _screens.RemoveAt(index);

            RectTransform.RemoveChild(enterScreen.transform);

            options.options.onLoaded?.Invoke(enterScreen, args);

            await enterScreen.AfterLoadAsync(RectTransform, args);

            ViewRef<Screen>? exitScreenRef = _screens.Count == 0 ? null : _screens[^1];
            Screen exitScreen = exitScreenRef.HasValue ? exitScreenRef.Value.View : null;
            var exitScreenId = exitScreen == false ? (int?) null : exitScreen.GetInstanceID();

            if (exitScreen)
            {
                exitScreen.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterScreen, exitScreen, args);
            }

            if (exitScreen)
            {
                await exitScreen.BeforeExitAsync(true, args);
            }

            await enterScreen.BeforeEnterAsync(true, args);

            // Play Animations
            var animExit = exitScreen
                ? exitScreen.ExitAsync(true, options.options.playAnimation, enterScreen)
                : default;

            var animEnter = enterScreen.EnterAsync(true, options.options.playAnimation, exitScreen);

            await UniTask.WhenAll(animExit, animEnter);

            // End Transition
            if (_isActiveScreenStacked == false && exitScreenId.HasValue)
            {
                _screens.RemoveAt(_screens.Count - 1);
            }

            _screens.Add(new ViewRef<Screen>(enterScreen, resourcePath, options.options.poolingPolicy));
            IsInTransition = false;

            // Postprocess
            if (exitScreen)
            {
                exitScreen.AfterExit(true, args);
            }

            enterScreen.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterScreen, exitScreen, args);
            }

            // Unload unused Screen
            if (_isActiveScreenStacked == false && exitScreenRef.HasValue)
            {
                await exitScreen.BeforeReleaseAsync(args);

                DestroyAndForget(exitScreenRef.Value);
            }

            _isActiveScreenStacked = options.stack;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TScreen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push<TScreen>(ScreenOptions options, params object[] args)
            where TScreen : Screen
        {
            PushAndForget<TScreen>(options, args).Forget();
        }
        
        /// <summary>
        /// Push an instance of <typeparamref name="TScreen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push<TScreen>(ScreenOptions options, Memory<object> args = default)
            where TScreen : Screen
        {
            PushAndForget<TScreen>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ScreenOptions options, params object[] args)
        {
            PushAndForget<Screen>(options, args).Forget();
        }
        
        /// <summary>
        /// Push an instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ScreenOptions options, Memory<object> args = default)
        {
            PushAndForget<Screen>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TScreen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync<TScreen>(ScreenOptions options, params object[] args)
            where TScreen : Screen
        {
            await PushAsyncInternal<TScreen>(options, args);
        }
        
        /// <summary>
        /// Push an instance of <typeparamref name="TScreen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync<TScreen>(ScreenOptions options, Memory<object> args = default)
            where TScreen : Screen
        {
            await PushAsyncInternal<TScreen>(options, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync(ScreenOptions options, params object[] args)
        {
            await PushAsyncInternal<Screen>(options, args);
        }
        
        /// <summary>
        /// Push an instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PushAsync(ScreenOptions options, Memory<object> args = default)
        {
            await PushAsyncInternal<Screen>(options, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid PushAndForget<TScreen>(ScreenOptions options, Memory<object> args)
            where TScreen : Screen
        {
            await PushAsyncInternal<Screen>(options, args);
        }

        private async UniTask PushAsyncInternal<TScreen>(ScreenOptions options, Memory<object> args)
            where TScreen : Screen
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

            var enterScreen = await GetViewAsync<TScreen>(options.options);
            options.options.onLoaded?.Invoke(enterScreen, args);

            await enterScreen.AfterLoadAsync(RectTransform, args);

            ViewRef<Screen>? exitScreenRef = _screens.Count == 0 ? null : _screens[^1];
            Screen exitScreen = exitScreenRef.HasValue ? exitScreenRef.Value.View : null;
            var exitScreenId = exitScreen == null ? (int?) null : exitScreen.GetInstanceID();

            if (exitScreen)
            {
                exitScreen.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterScreen, exitScreen, args);
            }

            if (exitScreen)
            {
                await exitScreen.BeforeExitAsync(true, args);
            }

            await enterScreen.BeforeEnterAsync(true, args);

            // Play Animations
            var animExit = exitScreen
                ? exitScreen.ExitAsync(true, options.options.playAnimation, enterScreen)
                : default;

            var animEnter = enterScreen.EnterAsync(true, options.options.playAnimation, exitScreen);

            await UniTask.WhenAll(animExit, animEnter);

            // End Transition
            if (_isActiveScreenStacked == false && exitScreenId.HasValue)
            {
                _screens.RemoveAt(_screens.Count - 1);
            }

            _screens.Add(new ViewRef<Screen>(enterScreen, resourcePath, options.options.poolingPolicy));
            IsInTransition = false;

            // Postprocess
            if (exitScreen)
            {
                exitScreen.AfterExit(true, args);
            }

            enterScreen.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterScreen, exitScreen, args);
            }

            // Unload unused Screen
            if (_isActiveScreenStacked == false && exitScreenRef.HasValue)
            {
                await exitScreen.BeforeReleaseAsync(args);

                DestroyAndForget(exitScreenRef.Value);
            }

            _isActiveScreenStacked = options.stack;
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Pop current instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pop(bool playAnimation, params object[] args)
        {
            PopAndForget(playAnimation, args).Forget();
        }
        
        /// <summary>
        /// Pop current instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pop(bool playAnimation, Memory<object> args = default)
        {
            PopAndForget(playAnimation, args).Forget();
        }

        /// <summary>
        /// Pop current instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask PopAsync(bool playAnimation, params object[] args)
        {
            await PopAsyncInternal(playAnimation, args);
        }
        
        /// <summary>
        /// Pop current instance of <see cref="Screen"/>.
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
            if (_screens.Count == 0)
            {
                ErrorIfCannotTransitionBecauseNoScreen();
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

            var lastScreen = _screens.Count - 1;
            var exitScreenRef = _screens[lastScreen];
            var exitScreen = exitScreenRef.View;
            exitScreen.Settings = Settings;

            var enterScreen = _screens.Count == 1 ? null : _screens[^2].View;

            if (enterScreen)
            {
                enterScreen.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePop(enterScreen, exitScreen, args);
            }

            await exitScreen.BeforeExitAsync(false, args);

            if (enterScreen)
            {
                await enterScreen.BeforeEnterAsync(false, args);
            }

            // Play Animations
            var animExit = exitScreen.ExitAsync(false, playAnimation, enterScreen);

            var animEnter = enterScreen
                ? enterScreen.EnterAsync(false, playAnimation, exitScreen)
                : default;

            await UniTask.WhenAll(animExit, animEnter);

            // End Transition
            _screens.RemoveAt(lastScreen);
            IsInTransition = false;

            // Postprocess
            exitScreen.AfterExit(false, args);

            if (enterScreen)
            {
                enterScreen.AfterEnter(false, args);
            }

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPop(enterScreen, exitScreen, args);
            }

            // Unload unused Screen
            await exitScreen.BeforeReleaseAsync(args);

            DestroyAndForget(exitScreenRef);

            _isActiveScreenStacked = true;
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorCannotFindParent(RectTransform rectTransform)
        {
            UnityEngine.Debug.LogError($"Cannot find any parent {nameof(ScreenContainer)} component", rectTransform);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotFindContainer(string containerName)
        {
            UnityEngine.Debug.LogError($"Cannot find any {nameof(ScreenContainer)} by name `{containerName}`");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotTransition()
        {
            UnityEngine.Debug.LogError("Cannot transition because there is a screen already in transition.");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotTransitionBecauseNoScreen()
        {
            UnityEngine.Debug.LogError("Cannot transition because there is no screen loaded on the stack.");
        }
    }
}