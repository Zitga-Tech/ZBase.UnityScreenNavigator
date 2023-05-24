using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public sealed class AnonymousScreenLifecycleEvent : IScreenLifecycleEvent
    {
        /// <inheritdoc cref="IScreenLifecycleEvent.DidPushEnter(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPushEnter;

        /// <inheritdoc cref="IScreenLifecycleEvent.DidPushExit(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPushExit;

        /// <inheritdoc cref="IScreenLifecycleEvent.DidPopEnter(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPopEnter;

        /// <inheritdoc cref="IScreenLifecycleEvent.DidPopExit(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidPopExit;
        
        public AnonymousScreenLifecycleEvent(
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

        /// <inheritdoc cref="IScreenLifecycleEvent.Initialize(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnInitialize { get; } = new();

        /// <inheritdoc cref="IScreenLifecycleEvent.WillPushEnter(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPushEnter { get; } = new();

        /// <inheritdoc cref="IScreenLifecycleEvent.WillPushExit(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPushExit { get; } = new();

        /// <inheritdoc cref="IScreenLifecycleEvent.WillPopEnter(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPopEnter { get; } = new();

        /// <inheritdoc cref="IScreenLifecycleEvent.WillPopExit(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillPopExit { get; } = new();

        /// <inheritdoc cref="IScreenLifecycleEvent.Cleanup"/>
        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask IScreenLifecycleEvent.Initialize(Memory<Arg> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPushEnter(Memory<Arg> args)
        {
            foreach (var onWillPushEnter in OnWillPushEnter)
                await onWillPushEnter.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPushEnter(Memory<Arg> args)
        {
            OnDidPushEnter?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPushExit(Memory<Arg> args)
        {
            foreach (var onWillPushExit in OnWillPushExit)
                await onWillPushExit.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPushExit(Memory<Arg> args)
        {
            OnDidPushExit?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPopEnter(Memory<Arg> args)
        {
            foreach (var onWillPopEnter in OnWillPopEnter)
                await onWillPopEnter.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPopEnter(Memory<Arg> args)
        {
            OnDidPopEnter?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPopExit(Memory<Arg> args)
        {
            foreach (var onWillPopExit in OnWillPopExit)
                await onWillPopExit.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPopExit(Memory<Arg> args)
        {
            OnDidPopExit?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}