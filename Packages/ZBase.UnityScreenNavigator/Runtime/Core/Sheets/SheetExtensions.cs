using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public static class SheetExtensions
    {
        public static void AddLifecycleEvent(
              this Sheet self
            , Func<Memory<Arg>, UniTask> initialize = null
            , Func<Memory<Arg>, UniTask> onWillEnter = null, Action<Memory<Arg>> onDidEnter = null
            , Func<Memory<Arg>, UniTask> onWillExit = null, Action<Memory<Arg>> onDidExit = null
            , Func<UniTask> onCleanup = null
            , int priority = 0
        )
        {
            var lifecycleEvent = new AnonymousSheetLifecycleEvent(
                initialize,
                onWillEnter, onDidEnter,
                onWillExit, onDidExit,
                onCleanup
            );

            self.AddLifecycleEvent(lifecycleEvent, priority);
        }
    }
}