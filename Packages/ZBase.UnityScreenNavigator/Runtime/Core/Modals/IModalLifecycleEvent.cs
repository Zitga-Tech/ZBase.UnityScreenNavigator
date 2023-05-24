using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public interface IModalLifecycleEvent
    {
        /// <summary>
        /// Call this method after the modal is loaded.
        /// </summary>
        /// <returns></returns>
        UniTask Initialize(Memory<Arg> args);

        /// <summary>
        /// Called just before this modal is displayed by the Push transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPushEnter(Memory<Arg> args);

        /// <summary>
        /// Called just after this modal is displayed by the Push transition.
        /// </summary>
        void DidPushEnter(Memory<Arg> args);

        /// <summary>
        /// Called just before this modal is hidden by the Push transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPushExit(Memory<Arg> args);

        /// <summary>
        /// Called just after this modal is hidden by the Push transition.
        /// </summary>
        void DidPushExit(Memory<Arg> args);

        /// <summary>
        /// Called just before this modal is displayed by the Pop transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPopEnter(Memory<Arg> args);

        /// <summary>
        /// Called just after this modal is displayed by the Pop transition.
        /// </summary>
        void DidPopEnter(Memory<Arg> args);

        /// <summary>
        /// Called just before this modal is hidden by the Pop transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPopExit(Memory<Arg> args);

        /// <summary>
        /// Called just after this modal is hidden by the Pop transition.
        /// </summary>
        void DidPopExit(Memory<Arg> args);

        /// <summary>
        /// Called just before this modal is released.
        /// </summary>
        /// <returns></returns>
        UniTask Cleanup();
    }
}