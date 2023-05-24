using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public static class ModalExtensions
    {
        public static void AddLifecycleEvent(
              this Modal self
            , Func<Memory<Arg>, UniTask> initialize = null
            , Func<Memory<Arg>, UniTask> onWillPushEnter = null, Action<Memory<Arg>> onDidPushEnter = null
            , Func<Memory<Arg>, UniTask> onWillPushExit = null, Action<Memory<Arg>> onDidPushExit = null
            , Func<Memory<Arg>, UniTask> onWillPopEnter = null, Action<Memory<Arg>> onDidPopEnter = null
            , Func<Memory<Arg>, UniTask> onWillPopExit = null, Action<Memory<Arg>> onDidPopExit = null
            , Func<UniTask> onCleanup = null
            , int priority = 0
        )
        {
            var lifecycleEvent = new AnonymousModalLifecycleEvent(
                initialize,
                onWillPushEnter, onDidPushEnter,
                onWillPushExit, onDidPushExit,
                onWillPopEnter, onDidPopEnter,
                onWillPopExit, onDidPopExit,
                onCleanup
            );

            self.AddLifecycleEvent(lifecycleEvent, priority);
        }
    }
}