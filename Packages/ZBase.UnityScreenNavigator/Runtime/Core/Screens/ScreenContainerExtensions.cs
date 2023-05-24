using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public static class ScreenContainerExtensions
    {
        /// <summary>
        /// Add callbacks.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="onBeforePush"></param>
        /// <param name="onAfterPush"></param>
        /// <param name="onBeforePop"></param>
        /// <param name="onAfterPop"></param>
        public static void AddCallbackReceiver(
              this ScreenContainer self
            , Action<Screen, Screen, Memory<Arg>> onBeforePush = null
            , Action<Screen, Screen, Memory<Arg>> onAfterPush = null
            , Action<Screen, Screen, Memory<Arg>> onBeforePop = null
            , Action<Screen, Screen, Memory<Arg>> onAfterPop = null
        )
        {
            var callbackReceiver = new AnonymousScreenContainerCallbackReceiver(
                onBeforePush, onAfterPush, onBeforePop, onAfterPop
            );

            self.AddCallbackReceiver(callbackReceiver);
        }

        /// <summary>
        /// Add callbacks.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="screen"></param>
        /// <param name="onBeforePush"></param>
        /// <param name="onAfterPush"></param>
        /// <param name="onBeforePop"></param>
        /// <param name="onAfterPop"></param>
        public static void AddCallbackReceiver(
              this ScreenContainer self
            , Screen screen
            , Action<Screen, Memory<Arg>> onBeforePush = null
            , Action<Screen, Memory<Arg>> onAfterPush = null
            , Action<Screen, Memory<Arg>> onBeforePop = null
            , Action<Screen, Memory<Arg>> onAfterPop = null
        )
        {
            var callbackReceiver = new AnonymousScreenContainerCallbackReceiver();

            callbackReceiver.OnBeforePush += (enterScreen, exitScreen, args) =>
            {
                if (enterScreen.Equals(screen))
                {
                    onBeforePush?.Invoke(exitScreen, args);
                }
            };

            callbackReceiver.OnAfterPush += (enterScreen, exitScreen, args) =>
            {
                if (enterScreen.Equals(screen))
                {
                    onAfterPush?.Invoke(exitScreen, args);
                }
            };

            callbackReceiver.OnBeforePop += (enterScreen, exitScreen, args) =>
            {
                if (exitScreen.Equals(screen))
                {
                    onBeforePop?.Invoke(enterScreen, args);
                }
            };

            callbackReceiver.OnAfterPop += (enterScreen, exitScreen, args) =>
            {
                if (exitScreen.Equals(screen))
                {
                    onAfterPop?.Invoke(enterScreen, args);
                }
            };

            var gameObj = self.gameObject;

            if (gameObj.TryGetComponent<MonoBehaviourDestroyedEventDispatcher>(out var destroyedEventDispatcher) == false)
            {
                destroyedEventDispatcher = gameObj.AddComponent<MonoBehaviourDestroyedEventDispatcher>();
            }

            destroyedEventDispatcher.OnDispatch += () => self.RemoveCallbackReceiver(callbackReceiver);

            self.AddCallbackReceiver(callbackReceiver);
        }
    }
}