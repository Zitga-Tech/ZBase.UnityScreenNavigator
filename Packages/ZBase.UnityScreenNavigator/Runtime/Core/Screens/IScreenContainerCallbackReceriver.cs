using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public interface IScreenContainerCallbackReceiver
    {
        void BeforePush(Screen enterScreen, Screen exitScreen, Memory<Arg> args);

        void AfterPush(Screen enterScreen, Screen exitScreen, Memory<Arg> args);

        void BeforePop(Screen enterScreen, Screen exitScreen, Memory<Arg> args);

        void AfterPop(Screen enterScreen, Screen exitScreen, Memory<Arg> args);
    }
}