using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public sealed class AnonymousSheetLifecycleEvent : ISheetLifecycleEvent
    {
        /// <see cref="ISheetLifecycleEvent.DidEnter(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidEnter;

        /// <see cref="ISheetLifecycleEvent.DidExit(Memory{Arg})"/>
        public event Action<Memory<Arg>> OnDidExit;

        public AnonymousSheetLifecycleEvent(
              Func<Memory<Arg>, UniTask> initialize = null
            , Func<Memory<Arg>, UniTask> onWillEnter = null, Action<Memory<Arg>> onDidEnter = null
            , Func<Memory<Arg>, UniTask> onWillExit = null, Action<Memory<Arg>> onDidExit = null
            , Func<UniTask> onCleanup = null
        )
        {
            if (initialize != null)
                OnInitialize.Add(initialize);

            if (onWillEnter != null)
                OnWillEnter.Add(onWillEnter);

            OnDidEnter = onDidEnter;

            if (onWillExit != null)
                OnWillExit.Add(onWillExit);

            OnDidExit = onDidExit;

            if (onCleanup != null)
                OnCleanup.Add(onCleanup);
        }

        /// <see cref="ISheetLifecycleEvent.Initialize(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnInitialize { get; } = new();

        /// <see cref="ISheetLifecycleEvent.WillEnter(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillEnter { get; } = new();

        /// <see cref="ISheetLifecycleEvent.WillExit(Memory{Arg})"/>
        public List<Func<Memory<Arg>, UniTask>> OnWillExit { get; } = new();

        /// <see cref="ISheetLifecycleEvent.Cleanup"/>
        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask ISheetLifecycleEvent.Initialize(Memory<Arg> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.WillEnter(Memory<Arg> args)
        {
            foreach (var onWillEnter in OnWillEnter)
                await onWillEnter.Invoke(args);
        }

        void ISheetLifecycleEvent.DidEnter(Memory<Arg> args)
        {
            OnDidEnter?.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.WillExit(Memory<Arg> args)
        {
            foreach (var onWillExit in OnWillExit)
                await onWillExit.Invoke(args);
        }

        void ISheetLifecycleEvent.DidExit(Memory<Arg> args)
        {
            OnDidExit?.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}