using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    public interface IActivityContainerCallbackReceiver
    {
        void BeforeShow(Activity activity, Memory<Arg> args);

        void AfterShow(Activity activity, Memory<Arg> args);

        void BeforeHide(Activity activity, Memory<Arg> args);

        void AfterHide(Activity activity, Memory<Arg> args);
    }
}