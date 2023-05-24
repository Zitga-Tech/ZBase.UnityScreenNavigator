using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public sealed class AnonymousModalLifecycleEvent : IModalLifecycleEvent
    {
        /// <see cref="IModalLifecycleEvent.DidPushEnter(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPushEnter;

        /// <see cref="IModalLifecycleEvent.DidPushExit(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPushExit;

        /// <see cref="IModalLifecycleEvent.DidPopEnter(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPopEnter;

        /// <see cref="IModalLifecycleEvent.DidPopExit(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPopExit;

        public AnonymousModalLifecycleEvent(
              Func<Memory<Arg>, UniTask> initialize = null
            , Func<Memory<Arg>, UniTask> onWillPushEnter = null, Action<Memory<Arg>> onDidPushEnter = null
            , Func<Memory<Arg>, UniTask> onWillPushExit = null, Action<Memory<Arg>> onDidPushExit = null
            , Func<Memory<Arg>, UniTask> onWillPopEnter = null, Action<Memory<Arg>> onDidPopEnter = null
            , Func<Memory<Arg>, UniTask> onWillPopExit = null, Action<Memory<Arg>> onDidPopExit = null
            , Func<UniTask> onCleanup = null
        )
        {
            if (initialize != null)
                OnInitialize.Add(initialize);

            if (onWillPushEnter != null)
                OnWillPushEnter.Add(onWillPushEnter);

            OnDidPushEnter = onDidPushEnter;

            if (onWillPushExit != null)
                OnWillPushExit.Add(onWillPushExit);

            OnDidPushExit = onDidPushExit;

            if (onWillPopEnter != null)
                OnWillPopEnter.Add(onWillPopEnter);

            OnDidPopEnter = onDidPopEnter;

            if (onWillPopExit != null)
                OnWillPopExit.Add(onWillPopExit);

            OnDidPopExit = onDidPopExit;

            if (onCleanup != null)
                OnCleanup.Add(onCleanup);
        }

        /// <see cref="IModalLifecycleEvent.Initialize(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnInitialize { get; } = new();

        /// <see cref="IModalLifecycleEvent.WillPushEnter(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPushEnter { get; } = new();

        /// <see cref="IModalLifecycleEvent.WillPushExit(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPushExit { get; } = new();

        /// <see cref="IModalLifecycleEvent.WillPopEnter(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPopEnter { get; } = new();

        /// <see cref="IModalLifecycleEvent.WillPopExit(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPopExit { get; } = new();

        /// <see cref="IModalLifecycleEvent.Cleanup"/>
        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask IModalLifecycleEvent.Initialize(Memory<Arg> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPushEnter(Memory<Arg> args)
        {
            foreach (var onWillPushEnter in OnWillPushEnter)
                await onWillPushEnter.Invoke(args);
        }

        void IModalLifecycleEvent.DidPushEnter(Memory<Arg> args)
        {
            OnDidPushEnter?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPushExit(Memory<Arg> args)
        {
            foreach (var onWillPushExit in OnWillPushExit)
                await onWillPushExit.Invoke(args);
        }

        void IModalLifecycleEvent.DidPushExit(Memory<Arg> args)
        {
            OnDidPushExit?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPopEnter(Memory<Arg> args)
        {
            foreach (var onWillPopEnter in OnWillPopEnter)
                await onWillPopEnter.Invoke(args);
        }

        void IModalLifecycleEvent.DidPopEnter(Memory<Arg> args)
        {
            OnDidPopEnter?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPopExit(Memory<Arg> args)
        {
            foreach (var onWillPopExit in OnWillPopExit)
                await onWillPopExit.Invoke(args);
        }

        void IModalLifecycleEvent.DidPopExit(Memory<Arg> args)
        {
            OnDidPopExit?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}