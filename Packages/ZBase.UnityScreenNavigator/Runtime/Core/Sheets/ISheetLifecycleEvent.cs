using System;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public interface ISheetLifecycleEvent
    {
        /// <summary>
        /// Called just after this sheet is loaded.
        /// </summary>
        /// <returns></returns>
        UniTask Initialize(Memory<object> args);

        /// <summary>
        /// Called just before this sheet is displayed by the Show transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillEnter(Memory<object> args);

        /// <summary>
        /// Called just after this sheet is displayed by the Show transition.
        /// </summary>
        /// <returns></returns>
        void DidEnter(Memory<object> args);

        /// <summary>
        /// Called just before this sheet is hidden by the Hide transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillExit(Memory<object> args);

        /// <summary>
        /// Called just after this sheet is hidden by the Hide transition.
        /// </summary>
        /// <returns></returns>
        void DidExit(Memory<object> args);

        /// <summary>
        /// Called just when this sheet is deinitialized.
        /// </summary>
        /// <returns></returns>
        void Deinitialize(Memory<object> args);

        /// <summary>
        /// Called just before this sheet is released.
        /// </summary>
        /// <returns></returns>
        UniTask Cleanup();
    }
}