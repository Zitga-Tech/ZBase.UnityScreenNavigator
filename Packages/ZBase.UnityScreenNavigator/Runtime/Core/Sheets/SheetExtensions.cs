using System;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public static class SheetExtensions
    {
        public static void AddLifecycleEvent(
              this Sheet self
            , Func<Memory<object>, UniTask> initialize = null
            , Func<Memory<object>, UniTask> onWillEnter = null, Action<Memory<object>> onDidEnter = null
            , Func<Memory<object>, UniTask> onWillExit = null, Action<Memory<object>> onDidExit = null
            , Action<Memory<object>> onDeinitialize = null, Func<UniTask> onCleanup = null
            , int priority = 0
        )
        {
            var lifecycleEvent = new AnonymousSheetLifecycleEvent(
                initialize,
                onWillEnter, onDidEnter,
                onWillExit, onDidExit,
                onDeinitialize, onCleanup
            );

            self.AddLifecycleEvent(lifecycleEvent, priority);
        }
    }
}