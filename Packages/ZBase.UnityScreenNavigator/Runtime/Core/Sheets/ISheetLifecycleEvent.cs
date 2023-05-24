using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public interface ISheetLifecycleEvent
    {
        /// <summary>
        /// Called just after this sheet is loaded.
        /// </summary>
        /// <returns></returns>
        UniTask Initialize(Memory<Arg> args);

        /// <summary>
        /// Called just before this sheet is displayed by the Show transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillEnter(Memory<Arg> args);

        /// <summary>
        /// Called just after this sheet is displayed by the Show transition.
        /// </summary>
        /// <returns></returns>
        void DidEnter(Memory<Arg> args);

        /// <summary>
        /// Called just before this sheet is hidden by the Hide transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillExit(Memory<Arg> args);

        /// <summary>
        /// Called just after this sheet is hidden by the Hide transition.
        /// </summary>
        /// <returns></returns>
        void DidExit(Memory<Arg> args);

        /// <summary>
        /// Called just before this sheet is released.
        /// </summary>
        /// <returns></returns>
        UniTask Cleanup();
    }
}