using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    public class AnonymousActivityContainerCallbackReceiver : IActivityContainerCallbackReceiver
    {
        public event Action<Activity, Memory<Arg>> OnAfterHide;
        public event Action<Activity, Memory<Arg>> OnAfterShow;
        public event Action<Activity, Memory<Arg>> OnBeforeHide;
        public event Action<Activity, Memory<Arg>> OnBeforeShow;

        public AnonymousActivityContainerCallbackReceiver(
              Action<Activity, Memory<Arg>> onBeforeShow = null
            , Action<Activity, Memory<Arg>> onAfterShow = null
            , Action<Activity, Memory<Arg>> onBeforeHide = null
            , Action<Activity, Memory<Arg>> onAfterHide = null
        )
        {
            OnBeforeShow = onBeforeShow;
            OnAfterShow = onAfterShow;
            OnBeforeHide = onBeforeHide;
            OnAfterHide = onAfterHide;
        }

        void IActivityContainerCallbackReceiver.BeforeShow(Activity activity, Memory<Arg> args)
        {
            OnBeforeShow?.Invoke(activity, args);
        }

        void IActivityContainerCallbackReceiver.AfterShow(Activity activity, Memory<Arg> args)
        {
            OnAfterShow?.Invoke(activity, args);
        }

        void IActivityContainerCallbackReceiver.BeforeHide(Activity activity, Memory<Arg> args)
        {
            OnBeforeHide?.Invoke(activity, args);
        }

        void IActivityContainerCallbackReceiver.AfterHide(Activity activity, Memory<Arg> args)
        {
            OnAfterHide?.Invoke(activity, args);
        }
    }
}