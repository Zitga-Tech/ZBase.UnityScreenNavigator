using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    public static class ActivityExtensions
    {
        public static IActivityLifecycleEvent AddLifecycleEvent(
              this Activity self
            , Func<Memory<Arg>, UniTask> initialize = null
            , Func<Memory<Arg>, UniTask> onWillShow = null, Action<Memory<Arg>> onDidShow = null
            , Func<Memory<Arg>, UniTask> onWillHide = null, Action<Memory<Arg>> onDidHide = null
            , Func<UniTask> onCleanup = null
            , int priority = 0
        )
        {
            var lifecycleEvent = new AnonymousActivityWindowLifecycleEvent(
                initialize,
                onWillShow, onDidShow,
                onWillHide, onDidHide,
                onCleanup
            );

            self.AddLifecycleEvent(lifecycleEvent, priority);
            return lifecycleEvent;
        }
    }
}