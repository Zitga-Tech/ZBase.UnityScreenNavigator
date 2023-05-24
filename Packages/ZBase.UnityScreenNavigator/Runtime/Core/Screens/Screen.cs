using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.PriorityCollection;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    [DisallowMultipleComponent]
    public class Screen : Window, IScreenLifecycleEvent
    {
        [SerializeField]
        private int _renderingOrder;

        [SerializeField]
        private ScreenTransitionAnimationContainer _animationContainer = new();

        private readonly UniquePriorityList<IScreenLifecycleEvent> _lifecycleEvents = new();
        private Progress<float> _transitionProgressReporter;

        private Progress<float> TransitionProgressReporter
        {
            get
            {
                return _transitionProgressReporter ??= new Progress<float>(SetTransitionProgress);
            }
        }

        public ScreenTransitionAnimationContainer AnimationContainer => _animationContainer;

        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Return the transition animation type currently playing.
        /// If not in transition, return null.
        /// </summary>
        public ScreenTransitionAnimationType? TransitionAnimationType { get; private set; }

        /// <summary>
        /// Progress of the transition animation.
        /// </summary>
        public float TransitionAnimationProgress { get; private set; }

        /// <summary>
        /// Event when the transition animation progress changes.
        /// </summary>
        public event Action<float> TransitionAnimationProgressChanged;

        /// <inheritdoc/>
        public virtual UniTask Initialize(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual UniTask WillPushEnter(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidPushEnter(Memory<Arg> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask WillPushExit(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidPushExit(Memory<Arg> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask WillPopEnter(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidPopEnter(Memory<Arg> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask WillPopExit(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidPopExit(Memory<Arg> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask Cleanup()
        {
            return UniTask.CompletedTask;
        }

        public void AddLifecycleEvent(IScreenLifecycleEvent lifecycleEvent, int priority = 0)
        {
            _lifecycleEvents.Add(lifecycleEvent, priority);
        }

        public void RemoveLifecycleEvent(IScreenLifecycleEvent lifecycleEvent)
        {
            _lifecycleEvents.Remove(lifecycleEvent);
        }

        internal async UniTask AfterLoadAsync(RectTransform parentTransform, Memory<Arg> args)
        {
            _lifecycleEvents.Add(this, 0);
            SetIdentifer();

            Parent = parentTransform;
            RectTransform.FillParent(Parent);

            // Set order of rendering.
            var siblingIndex = 0;

            for (var i = 0; i < Parent.childCount; i++)
            {
                var child = Parent.GetChild(i);
                var childScreen = child.GetComponent<Screen>();

                siblingIndex = i;

                if (_renderingOrder >= childScreen._renderingOrder)
                {
                    continue;
                }

                break;
            }

            RectTransform.SetSiblingIndex(siblingIndex);
            Alpha = 0.0f;

            var tasks = _lifecycleEvents.Select(x => x.Initialize(args));
            await WaitForAsync(tasks);
        }

        internal async UniTask BeforeEnterAsync(bool push, Memory<Arg> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = push ? ScreenTransitionAnimationType.PushEnter : ScreenTransitionAnimationType.PopEnter;
            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            SetTransitionProgress(0.0f);

            Alpha = 0.0f;

            var tasks = push
                ? _lifecycleEvents.Select(x => x.WillPushEnter(args))
                : _lifecycleEvents.Select(x => x.WillPopEnter(args));
            
            await WaitForAsync(tasks);
        }

        internal async UniTask EnterAsync(bool push, bool playAnimation, Screen partnerScreen)
        {
            Alpha = 1.0f;

            if (playAnimation)
            {
                var anim = GetAnimation(push, true, partnerScreen);

                if (partnerScreen)
                {
                    anim.SetPartner(partnerScreen.RectTransform);
                }

                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
            }

            RectTransform.FillParent(Parent);
            SetTransitionProgress(1.0f);
        }

        internal void AfterEnter(bool push, Memory<Arg> args)
        {
            if (push)
            {
                foreach (var lifecycleEvent in _lifecycleEvents)
                {
                    lifecycleEvent.DidPushEnter(args);
                }
            }
            else
            {
                foreach (var lifecycleEvent in _lifecycleEvents)
                {
                    lifecycleEvent.DidPopEnter(args);
                }
            }

            IsTransitioning = false;
            TransitionAnimationType = null;
        }

        internal async UniTask BeforeExitAsync(bool push, Memory<Arg> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = push
                ? ScreenTransitionAnimationType.PushExit
                : ScreenTransitionAnimationType.PopExit;

            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            SetTransitionProgress(0.0f);

            Alpha = 1.0f;

            var tasks = push
                ? _lifecycleEvents.Select(x => x.WillPushExit(args))
                : _lifecycleEvents.Select(x => x.WillPopExit(args));

            await WaitForAsync(tasks);
        }

        internal async UniTask ExitAsync(bool push, bool playAnimation, Screen partnerScreen)
        {
            if (playAnimation)
            {
                var anim = GetAnimation(push, false, partnerScreen);

                if (partnerScreen)
                {
                    anim.SetPartner(partnerScreen.RectTransform);
                }

                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
            }
            
            Alpha = 0.0f;
            SetTransitionProgress(1.0f);
        }

        internal void AfterExit(bool push, Memory<Arg> args)
        {
            if (push)
            {
                foreach (var lifecycleEvent in _lifecycleEvents)
                {
                    lifecycleEvent.DidPushExit(args);
                }
            }
            else
            {
                foreach (var lifecycleEvent in _lifecycleEvents)
                {
                    lifecycleEvent.DidPopExit(args);
                }
            }

            gameObject.SetActive(false);
            IsTransitioning = false;
            TransitionAnimationType = null;
        }

        internal async UniTask BeforeReleaseAsync()
        {
            var tasks = _lifecycleEvents.Select(x => x.Cleanup());
            await WaitForAsync(tasks);
        }

        private void SetTransitionProgress(float progress)
        {
            TransitionAnimationProgress = progress;
            TransitionAnimationProgressChanged?.Invoke(progress);
        }

        private ITransitionAnimation GetAnimation(bool push, bool enter, Screen partner)
        {
            var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
            var anim = _animationContainer.GetAnimation(push, enter, partnerIdentifier);

            if (anim == null)
            {
                return Settings.GetDefaultScreenTransitionAnimation(push, enter);
            }

            return anim;
        }
    }
}