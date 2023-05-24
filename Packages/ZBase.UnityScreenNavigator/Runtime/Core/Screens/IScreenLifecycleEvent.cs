using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public interface IScreenLifecycleEvent
    {
        /// <summary>
        /// Called just after this screen is loaded.
        /// </summary>
        /// <returns></returns>
        UniTask Initialize(Memory<Arg> args);

        /// <summary>
        /// Called just before this screen is displayed by the Push transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPushEnter(Memory<Arg> args);

        /// <summary>
        /// Called just after this screen is displayed by the Push transition.
        /// </summary>
        void DidPushEnter(Memory<Arg> args);

        /// <summary>
        /// Called just before this screen is hidden by the Push transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPushExit(Memory<Arg> args);

        /// <summary>
        /// Called just after this screen is hidden by the Push transition.
        /// </summary>
        void DidPushExit(Memory<Arg> args);

        /// <summary>
        /// Called just before this screen is displayed by the Pop transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPopEnter(Memory<Arg> args);

        /// <summary>
        /// Called just after this screen is displayed by the Pop transition.
        /// </summary>
        void DidPopEnter(Memory<Arg> args);

        /// <summary>
        /// Called just before this screen is hidden by the Pop transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillPopExit(Memory<Arg> args);

        /// <summary>
        /// Called just after this screen is hidden by the Pop transition.
        /// </summary>
        void DidPopExit(Memory<Arg> args);

        /// <summary>
        /// Called just before this screen is released.
        /// </summary>
        /// <returns></returns>
        UniTask Cleanup();
    }
}