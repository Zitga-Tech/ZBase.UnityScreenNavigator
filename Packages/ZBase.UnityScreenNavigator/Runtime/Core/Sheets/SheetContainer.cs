using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Controls;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [RequireComponent(typeof(RectMask2D), typeof(CanvasGroup))]
    public class SheetContainer : ControlContainerBase
    {
        private readonly List<ISheetContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<int, ViewRef<Sheet>> _sheets = new();
        private int? _activeSheetId;

        public IReadOnlyDictionary<int, ViewRef<Sheet>> Sheets => _sheets;

        public int? ActiveSheetId => _activeSheetId;

        public Sheet ActiveSheet
        {
            get
            {
                if (ActiveSheetId.HasValue == false)
                {
                    return null;
                }

                return _sheets[ActiveSheetId.Value].View;
            }
        }

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            _callbackReceivers.AddRange(GetComponents<ISheetContainerCallbackReceiver>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cleanup(params object[] args)
        {
            CleanupAndForget(args).Forget();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cleanup(Memory<object> args = default)
        {
            CleanupAndForget(args).Forget();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask CleanupAsync(params object[] args)
        {
            await CleanupAsyncInternal(args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask CleanupAsync(Memory<object> args = default)
        {
            await CleanupAsyncInternal(args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid CleanupAndForget(Memory<object> args)
        {
            await CleanupAsyncInternal(args);
        }

        private async UniTask CleanupAsyncInternal(Memory<object> args)
        {
            _activeSheetId = null;
            IsInTransition = false;

            var sheets = _sheets;

            foreach (var sheetRef in sheets.Values)
            {
                await sheetRef.View.BeforeReleaseAsync(args);
                DestroyAndForget(sheetRef);
            }

            sheets.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            var controls = _sheets;

            foreach (var controlRef in controls.Values)
            {
                var (control, resourcePath) = controlRef;
                DestroyAndForget(control, resourcePath, PoolingPolicy.DisablePooling).Forget();
            }

            controls.Clear();
        }

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(ISheetContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(ISheetContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register<TSheet>(SheetOptions options, params object[] args)
            where TSheet : Sheet
        {
            RegisterAndForget<TSheet>(options, args).Forget();
        }
        
        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register<TSheet>(SheetOptions options, Memory<object> args = default)
            where TSheet : Sheet
        {
            RegisterAndForget<TSheet>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(SheetOptions options, params object[] args)
        {
            RegisterAndForget<Sheet>(options, args).Forget();
        }
        
        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(SheetOptions options, Memory<object> args = default)
        {
            RegisterAndForget<Sheet>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask<int> RegisterAsync<TSheet>(SheetOptions options, params object[] args)
            where TSheet : Sheet
        {
            return await RegisterAsyncInternal<TSheet>(options, args);
        }
        
        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask<int> RegisterAsync<TSheet>(SheetOptions options, Memory<object> args = default)
            where TSheet : Sheet
        {
            return await RegisterAsyncInternal<TSheet>(options, args);
        }

        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask<int> RegisterAsync(SheetOptions options, params object[] args)
        {
            return await RegisterAsyncInternal<Sheet>(options, args);
        }
        
        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask<int> RegisterAsync(SheetOptions options, Memory<object> args = default)
        {
            return await RegisterAsyncInternal<Sheet>(options, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid RegisterAndForget<TSheet>(SheetOptions options, Memory<object> args)
            where TSheet : Sheet
        {
            await RegisterAsyncInternal<TSheet>(options, args);
        }

        private async UniTask<int> RegisterAsyncInternal<TSheet>(SheetOptions options, Memory<object> args)
            where TSheet : Sheet
        {
            var resourcePath = options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var (sheetId, sheet) = await GetSheetAsync<TSheet>(options);

            options.onLoaded?.Invoke(sheetId, sheet, args);

            await sheet.AfterLoadAsync((RectTransform)transform, args);

            return sheetId;
        }

        private async UniTask<(int, TSheet)> GetSheetAsync<TSheet>(SheetOptions options)
            where TSheet : Sheet
        {
            var sheet = await GetViewAsync<TSheet>(options.AsViewOptions());
            var sheetId = sheet.GetInstanceID();

            this._sheets[sheetId] = new ViewRef<Sheet>(sheet, options.resourcePath, options.poolingPolicy);

            return (sheetId, sheet);
        }

        /// <summary>
        /// Show an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Show(int sheetId, bool playAnimation, params object[] args)
        {
            ShowAndForget(sheetId, playAnimation, args).Forget();
        }
        
        /// <summary>
        /// Show an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Show(int sheetId, bool playAnimation, Memory<object> args = default)
        {
            ShowAndForget(sheetId, playAnimation, args).Forget();
        }

        /// <summary>
        /// Show an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask ShowAsync(int sheetId, bool playAnimation, params object[] args)
        {
            await ShowAsyncInternal(sheetId, playAnimation, args);
        }
        
        /// <summary>
        /// Show an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask ShowAsync(int sheetId, bool playAnimation, Memory<object> args = default)
        {
            await ShowAsyncInternal(sheetId, playAnimation, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid ShowAndForget(int sheetId, bool playAnimation, Memory<object> args)
        {
            await ShowAsyncInternal(sheetId, playAnimation, args);
        }

        private async UniTask ShowAsyncInternal(int sheetId, bool playAnimation, Memory<object> args)
        {
            if (IsInTransition)
            {
                ErrorIfCannotTransition();
                return;
            }

            if (ActiveSheetId.HasValue && ActiveSheetId.Value.Equals(sheetId))
            {
                WarningIfCannotTransitionBecauseActive(sheetId);
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var enterSheet = _sheets[sheetId].View;
            enterSheet.Settings = Settings;

            ViewRef<Sheet>? exitSheetRef = this.ActiveSheetId.HasValue ? this._sheets[this.ActiveSheetId.Value] : null;
            var exitSheet = exitSheetRef.HasValue ? exitSheetRef.Value.View : null;

            if (exitSheet)
            {
                exitSheet.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeShow(enterSheet, exitSheet, args);
            }

            if (exitSheet)
            {
                await exitSheet.BeforeExitAsync(args);
            }

            await enterSheet.BeforeEnterAsync(args);

            // Play Animation
            var animExit = exitSheet
                ? exitSheet.ExitAsync(playAnimation, enterSheet)
                : default;

            var animEnter = enterSheet.EnterAsync(playAnimation, exitSheet);

            await UniTask.WhenAll(animExit, animEnter);

            // End Transition
            _activeSheetId = sheetId;
            IsInTransition = false;

            // Postprocess
            if (exitSheet)
            {
                exitSheet.AfterExit(args);
            }

            enterSheet.AfterEnter(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterShow(enterSheet, exitSheet, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Hide an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hide(bool playAnimation, params object[] args)
        {
            HideAndForget(playAnimation, args).Forget();
        }
        
        /// <summary>
        /// Hide an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hide(bool playAnimation, Memory<object> args = default)
        {
            HideAndForget(playAnimation, args).Forget();
        }

        /// <summary>
        /// Hide an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask HideAsync(bool playAnimation, params object[] args)
        {
            await HideAsyncInternal(playAnimation, args);
        }
        
        /// <summary>
        /// Hide an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask HideAsync(bool playAnimation, Memory<object> args = default)
        {
            await HideAsyncInternal(playAnimation, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async UniTaskVoid HideAndForget(bool playAnimation, Memory<object> args)
        {
            await HideAsyncInternal(playAnimation, args);
        }

        private async UniTask HideAsyncInternal(bool playAnimation, Memory<object> args)
        {
            if (IsInTransition)
            {
                ErrorIfCannotTransition();
                return;
            }

            if (ActiveSheetId.HasValue == false)
            {
                WarningIfCannotTransitionBecauseNoActive();
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var exitSheetRef = _sheets[ActiveSheetId.Value];
            var exitSheet = exitSheetRef.View;
            exitSheet.Settings = Settings;

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeHide(exitSheet, args);
            }

            await exitSheet.BeforeExitAsync(args);

            // Play Animation
            await exitSheet.ExitAsync(playAnimation, null);

            // End Transition
            _activeSheetId = null;
            IsInTransition = false;

            // Postprocess
            exitSheet.AfterExit(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterHide(exitSheet, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorIfCannotTransition()
        {
            UnityEngine.Debug.LogError("Cannot transition because there is a sheet already in transition.");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void WarningIfCannotTransitionBecauseActive(int sheetId)
        {
            UnityEngine.Debug.LogWarning($"Cannot transition because the sheet {sheetId} is already active.");
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void WarningIfCannotTransitionBecauseNoActive()
        {
            UnityEngine.Debug.LogWarning("Cannot transition because there is no active sheet.");
        }
    }
}