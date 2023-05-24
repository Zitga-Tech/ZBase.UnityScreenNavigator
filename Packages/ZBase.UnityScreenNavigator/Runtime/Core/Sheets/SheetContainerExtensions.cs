using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public static class SheetContainerExtensions
    {
        /// <summary>
        /// Add callbacks.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="onBeforeShow"></param>
        /// <param name="onAfterShow"></param>
        /// <param name="onBeforeHide"></param>
        /// <param name="onAfterHide"></param>
        public static void AddCallbackReceiver(this SheetContainer self
            , Action<Sheet, Sheet, Memory<Arg>> onBeforeShow = null
            , Action<Sheet, Sheet, Memory<Arg>> onAfterShow = null
            , Action<Sheet, Memory<Arg>> onBeforeHide = null
            , Action<Sheet, Memory<Arg>> onAfterHide = null
        )
        {
            var callbackReceiver = new AnonymousSheetContainerCallbackReceiver(
                onBeforeShow, onAfterShow, onBeforeHide, onAfterHide
            );

            self.AddCallbackReceiver(callbackReceiver);
        }
    }
}