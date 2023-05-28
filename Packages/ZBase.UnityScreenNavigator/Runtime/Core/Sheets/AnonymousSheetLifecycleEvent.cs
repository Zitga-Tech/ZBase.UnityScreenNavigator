using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public sealed class AnonymousSheetLifecycleEvent : ISheetLifecycleEvent
    {
        /// <see cref="ISheetLifecycleEvent.DidEnter(Memory{object})"/>
        public event Action<Memory<object>> OnDidEnter;

        /// <see cref="ISheetLifecycleEvent.DidExit(Memory{object})"/>
        public event Action<Memory<object>> OnDidExit;

        /// <see cref="ISheetLifecycleEvent.Deinitialize(Memory{object})"/>
        public event Action<Memory<object>> OnDeinitialize;

        public AnonymousSheetLifecycleEvent(
              Func<Memory<object>, UniTask> initialize = null
            , Func<Memory<object>, UniTask> onWillEnter = null, Action<Memory<object>> onDidEnter = null
            , Func<Memory<object>, UniTask> onWillExit = null, Action<Memory<object>> onDidExit = null
            , Action<Memory<object>> onDeinitialize = null, Func<UniTask> onCleanup = null
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
            OnDeinitialize = onDeinitialize;

            if (onCleanup != null)
                OnCleanup.Add(onCleanup);
        }

        /// <see cref="ISheetLifecycleEvent.Initialize(Memory{object})"/>
        public List<Func<Memory<object>, UniTask>> OnInitialize { get; } = new();

        /// <see cref="ISheetLifecycleEvent.WillEnter(Memory{object})"/>
        public List<Func<Memory<object>, UniTask>> OnWillEnter { get; } = new();

        /// <see cref="ISheetLifecycleEvent.WillExit(Memory{object})"/>
        public List<Func<Memory<object>, UniTask>> OnWillExit { get; } = new();

        /// <see cref="ISheetLifecycleEvent.Cleanup"/>
        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask ISheetLifecycleEvent.Initialize(Memory<object> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.WillEnter(Memory<object> args)
        {
            foreach (var onWillEnter in OnWillEnter)
                await onWillEnter.Invoke(args);
        }

        void ISheetLifecycleEvent.DidEnter(Memory<object> args)
        {
            OnDidEnter?.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.WillExit(Memory<object> args)
        {
            foreach (var onWillExit in OnWillExit)
                await onWillExit.Invoke(args);
        }

        void ISheetLifecycleEvent.DidExit(Memory<object> args)
        {
            OnDidExit?.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }

        void ISheetLifecycleEvent.Deinitialize(Memory<object> args)
        {
            OnDeinitialize?.Invoke(args);
        }
    }
}