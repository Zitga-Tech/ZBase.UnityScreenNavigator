using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    public sealed class AnonymousActivityWindowLifecycleEvent : IActivityLifecycleEvent
    {
        /// <see cref="IActivityLifecycleEvent.DidShow(Memory{object})"/>
        public event Action<Memory<Arg>> OnDidShow;

        /// <see cref="IActivityLifecycleEvent.DidHide(Memory{object})"/>
        public event Action<Memory<Arg>> OnDidHide;

        public AnonymousActivityWindowLifecycleEvent(
              Func<Memory<Arg>, UniTask> initialize = null
            , Func<Memory<Arg>, UniTask> onWillShow = null, Action<Memory<Arg>> onDidShow = null
            , Func<Memory<Arg>, UniTask> onWillHide = null, Action<Memory<Arg>> onDidHide = null
            , Func<UniTask> onCleanup = null
        )
        {
            if (initialize != null)
                OnInitialize.Add(initialize);

            if (onWillShow != null)
                OnWillShow.Add(onWillShow);

            OnDidShow = onDidShow;

            if (onWillHide != null)
                OnWillHide.Add(onWillHide);

            OnDidHide = onDidHide;

            if (onCleanup != null)
                OnCleanup.Add(onCleanup);
        }

        /// <see cref="IActivityLifecycleEvent.Initialize(Memory{object})"/>
        public List<Func<Memory<Arg>, UniTask>> OnInitialize { get; } = new();

        /// <see cref="IActivityLifecycleEvent.WillShow(Memory{object})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillShow { get; } = new();

        /// <see cref="IActivityLifecycleEvent.WillHide(Memory{object})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillHide { get; } = new();

        /// <see cref="IActivityLifecycleEvent.Cleanup"/>
        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask IActivityLifecycleEvent.Initialize(Memory<Arg> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask IActivityLifecycleEvent.WillShow(Memory<Arg> args)
        {
            foreach (var onWillShowEnter in OnWillShow)
                await onWillShowEnter.Invoke(args);
        }

        void IActivityLifecycleEvent.DidShow(Memory<Arg> args)
        {
            OnDidShow?.Invoke(args);
        }

        async UniTask IActivityLifecycleEvent.WillHide(Memory<Arg> args)
        {
            foreach (var onWillHideEnter in OnWillHide)
                await onWillHideEnter.Invoke(args);
        }

        void IActivityLifecycleEvent.DidHide(Memory<Arg> args)
        {
            OnDidHide?.Invoke(args);
        }

        async UniTask IActivityLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}