using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public interface ISheetContainerCallbackReceiver
    {
        void BeforeShow(Sheet enterSheet, Sheet exitSheet, Memory<Arg> args);

        void AfterShow(Sheet enterSheet, Sheet exitSheet, Memory<Arg> args);

        void BeforeHide(Sheet exitSheet, Memory<Arg> args);

        void AfterHide(Sheet exitSheet, Memory<Arg> args);
    }
}