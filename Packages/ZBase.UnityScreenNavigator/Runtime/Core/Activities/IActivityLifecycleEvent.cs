using System;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    public interface IActivityLifecycleEvent
    {
        /// <summary>
        /// Call this method after the activity is loaded.
        /// </summary>
        /// <returns></returns>
        UniTask Initialize(Memory<Arg> args);

        /// <summary>
        /// Called just before this activity is displayed by the Show transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillShow(Memory<Arg> args);

        /// <summary>
        /// Called just after this activity is displayed by the Show transition.
        /// </summary>
        void DidShow(Memory<Arg> args);

        /// <summary>
        /// Called just before this activity is hidden by the Hide transition.
        /// </summary>
        /// <returns></returns>
        UniTask WillHide(Memory<Arg> args);

        /// <summary>
        /// Called just after this activity is hidden by the Hide transition.
        /// </summary>
        void DidHide(Memory<Arg> args);

        /// <summary>
        /// Called just before this activity is released.
        /// </summary>
        /// <returns></returns>
        UniTask Cleanup();
    }
}