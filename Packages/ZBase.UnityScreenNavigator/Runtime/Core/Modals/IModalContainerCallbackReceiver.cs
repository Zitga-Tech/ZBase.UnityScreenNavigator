using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public interface IModalContainerCallbackReceiver
    {
        void BeforePush(Modal enterModal, Modal exitModal, Memory<Arg> args);

        void AfterPush(Modal enterModal, Modal exitModal, Memory<Arg> args);

        void BeforePop(Modal enterModal, Modal exitModal, Memory<Arg> args);

        void AfterPop(Modal enterModal, Modal exitModal, Memory<Arg> args);
    }
}