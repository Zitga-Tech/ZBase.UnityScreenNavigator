using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public sealed class AnonymousScreenContainerCallbackReceiver : IScreenContainerCallbackReceiver
    {
        public event Action<Screen, Screen, Memory<Arg>> OnAfterPop;
        public event Action<Screen, Screen, Memory<Arg>> OnAfterPush;
        public event Action<Screen, Screen, Memory<Arg>> OnBeforePop;
        public event Action<Screen, Screen, Memory<Arg>> OnBeforePush;

        public AnonymousScreenContainerCallbackReceiver(
              Action<Screen, Screen, Memory<Arg>> onBeforePush = null
            , Action<Screen, Screen, Memory<Arg>> onAfterPush = null
            , Action<Screen, Screen, Memory<Arg>> onBeforePop = null
            , Action<Screen, Screen, Memory<Arg>> onAfterPop = null
        )
        {
            OnBeforePush = onBeforePush;
            OnAfterPush = onAfterPush;
            OnBeforePop = onBeforePop;
            OnAfterPop = onAfterPop;
        }

        void IScreenContainerCallbackReceiver.BeforePush(Screen enterScreen, Screen exitScreen, Memory<Arg> args)
        {
            OnBeforePush?.Invoke(enterScreen, exitScreen, args);
        }

        void IScreenContainerCallbackReceiver.AfterPush(Screen enterScreen, Screen exitScreen, Memory<Arg> args)
        {
            OnAfterPush?.Invoke(enterScreen, exitScreen, args);
        }

        void IScreenContainerCallbackReceiver.BeforePop(Screen enterScreen, Screen exitScreen, Memory<Arg> args)
        {
            OnBeforePop?.Invoke(enterScreen, exitScreen, args);
        }

        void IScreenContainerCallbackReceiver.AfterPop(Screen enterScreen, Screen exitScreen, Memory<Arg> args)
        {
            OnAfterPop?.Invoke(enterScreen, exitScreen, args);
        }
    }
}