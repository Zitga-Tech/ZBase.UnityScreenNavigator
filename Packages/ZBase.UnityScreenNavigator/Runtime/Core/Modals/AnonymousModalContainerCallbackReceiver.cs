using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public sealed class AnonymousModalContainerCallbackReceiver : IModalContainerCallbackReceiver
    {
        public event Action<Modal, Modal, Memory<Arg>> OnAfterPop;
        public event Action<Modal, Modal, Memory<Arg>> OnAfterPush;
        public event Action<Modal, Modal, Memory<Arg>> OnBeforePop;
        public event Action<Modal, Modal, Memory<Arg>> OnBeforePush;

        public AnonymousModalContainerCallbackReceiver(
              Action<Modal, Modal, Memory<Arg>> onBeforePush = null
            , Action<Modal, Modal, Memory<Arg>> onAfterPush = null
            , Action<Modal, Modal, Memory<Arg>> onBeforePop = null
            , Action<Modal, Modal, Memory<Arg>> onAfterPop = null
        )
        {
            OnBeforePush = onBeforePush;
            OnAfterPush = onAfterPush;
            OnBeforePop = onBeforePop;
            OnAfterPop = onAfterPop;
        }

        void IModalContainerCallbackReceiver.BeforePush(Modal enterModal, Modal exitModal, Memory<Arg> args)
        {
            OnBeforePush?.Invoke(enterModal, exitModal, args);
        }

        void IModalContainerCallbackReceiver.AfterPush(Modal enterModal, Modal exitModal, Memory<Arg> args)
        {
            OnAfterPush?.Invoke(enterModal, exitModal, args);
        }

        void IModalContainerCallbackReceiver.BeforePop(Modal enterModal, Modal exitModal, Memory<Arg> args)
        {
            OnBeforePop?.Invoke(enterModal, exitModal, args);
        }

        void IModalContainerCallbackReceiver.AfterPop(Modal enterModal, Modal exitModal, Memory<Arg> args)
        {
            OnAfterPop?.Invoke(enterModal, exitModal, args);
        }
    }
}