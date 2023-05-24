using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public sealed class AnonymousSheetContainerCallbackReceiver : ISheetContainerCallbackReceiver
    {
        public event Action<Sheet, Sheet, Memory<Arg>> OnBeforeShow;
        public event Action<Sheet, Sheet, Memory<Arg>> OnAfterShow;
        public event Action<Sheet, Memory<Arg>> OnBeforeHide;
        public event Action<Sheet, Memory<Arg>> OnAfterHide;

        public AnonymousSheetContainerCallbackReceiver(
              Action<Sheet, Sheet, Memory<Arg>> onBeforeShow = null
            , Action<Sheet, Sheet, Memory<Arg>> onAfterShow = null
            , Action<Sheet, Memory<Arg>> onBeforeHide = null
            , Action<Sheet, Memory<Arg>> onAfterHide = null
        )
        {
            OnBeforeShow = onBeforeShow;
            OnAfterShow = onAfterShow;
            OnBeforeHide = onBeforeHide;
            OnAfterHide = onAfterHide;
        }

        void ISheetContainerCallbackReceiver.BeforeShow(Sheet enterSheet, Sheet exitSheet, Memory<Arg> args)
        {
            OnBeforeShow?.Invoke(enterSheet, exitSheet, args);
        }

        void ISheetContainerCallbackReceiver.AfterShow(Sheet enterSheet, Sheet exitSheet, Memory<Arg> args)
        {
            OnAfterShow?.Invoke(enterSheet, exitSheet, args);
        }

        void ISheetContainerCallbackReceiver.BeforeHide(Sheet exitSheet, Memory<Arg> args)
        {
            OnBeforeHide?.Invoke(exitSheet, args);
        }

        void ISheetContainerCallbackReceiver.AfterHide(Sheet exitSheet, Memory<Arg> args)
        {
            OnAfterHide?.Invoke(exitSheet, args);
        }
    }
}