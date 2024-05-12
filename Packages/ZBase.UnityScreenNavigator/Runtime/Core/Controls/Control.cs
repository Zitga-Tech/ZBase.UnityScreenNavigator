using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.PriorityCollection;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    [DisallowMultipleComponent]
    public class Control : View, IControlLifecycleEvent
    {
        [SerializeField]
        private ControlTransitionAnimationContainer _animationContainer = new();

        private readonly UniquePriorityList<IControlLifecycleEvent> _lifecycleEvents = new();
        private Progress<float> _transitionProgressReporter;

        public ControlTransitionAnimationContainer AnimationContainer
        {
            get => _animationContainer;
        }

        private Progress<float> TransitionProgressReporter
        {
            get => _transitionProgressReporter ??= new Progress<float>(SetTransitionProgress);
        }

        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Return the transition animation type currently playing.
        /// If not in transition, return null.
        /// </summary>
        public ControlTransitionAnimationType? TransitionAnimationType { get; private set; }

        /// <summary>
        /// Progress of the transition animation.
        /// </summary>
        public float TransitionAnimationProgress { get; private set; }

        /// <summary>
        /// Event when the transition animation progress changes.
        /// </summary>
        public event Action<float> TransitionAnimationProgressChanged;

        /// <inheritdoc/>
        public virtual UniTask Initialize(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual UniTask WillEnter(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidEnter(Memory<object> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask WillExit(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidExit(Memory<object> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask Cleanup(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        public void AddLifecycleEvent(IControlLifecycleEvent lifecycleEvent, int priority = 0)
        {
            _lifecycleEvents.Add(lifecycleEvent, priority);
        }

        public void RemoveLifecycleEvent(IControlLifecycleEvent lifecycleEvent)
        {
            _lifecycleEvents.Remove(lifecycleEvent);
        }

        internal async UniTask AfterLoadAsync(RectTransform parentTransform, Memory<object> args)
        {
            _lifecycleEvents.Add(this, 0);
            SetIdentifer();

            Parent = parentTransform;
            gameObject.SetActive(false);

            OnAfterLoad();

            var tasks = _lifecycleEvents.Select(x => x.Initialize(args));
            await WaitForAsync(tasks);
        }

        protected virtual void OnAfterLoad()
        {
            RectTransform.SetParentRect(Parent);
        }

        internal async UniTask BeforeEnterAsync(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = ControlTransitionAnimationType.Enter;
            gameObject.SetActive(true);

            OnBeforeEnter();

            var tasks = _lifecycleEvents.Select(x => x.WillEnter(args));
            await WaitForAsync(tasks);
        }

        protected virtual void OnBeforeEnter()
        {
            RectTransform.SetParentRect(Parent);
        }

        internal async UniTask<StubEnter> EnterAsync(bool playAnimation, Control partnerControl)
        {
            OnEnter();

            if (playAnimation == false)
            {
                return default;
            }

            var anim = GetAnimation(true, partnerControl);

            if (anim == null)
            {
                return default;
            }

            if (partnerControl)
            {
                anim.SetPartner(partnerControl.RectTransform);
            }

            anim.Setup(RectTransform);

            await anim.PlayAsync(TransitionProgressReporter);

            return default;
        }

        protected virtual void OnEnter() { }

        internal void AfterEnter(Memory<object> args)
        {
            foreach (var lifecycleEvent in _lifecycleEvents)
            {
                lifecycleEvent.DidEnter(args);
            }

            IsTransitioning = false;
            TransitionAnimationType = null;
        }

        internal async UniTask BeforeExitAsync(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = ControlTransitionAnimationType.Exit;
            gameObject.SetActive(true);

            OnBeforeExit();

            var tasks = _lifecycleEvents.Select(x => x.WillExit(args));
            await WaitForAsync(tasks);
        }

        protected virtual void OnBeforeExit()
        {
            RectTransform.SetParentRect(Parent);
        }

        internal async UniTask<StubExit> ExitAsync(bool playAnimation, Control partnerControl)
        {
            if (playAnimation == false)
            {
                return default;
            }

            var anim = GetAnimation(false, partnerControl);

            if (anim == null)
            {
                return default;
            }

            if (partnerControl)
            {
                anim.SetPartner(partnerControl.RectTransform);
            }

            anim.Setup(RectTransform);

            await anim.PlayAsync(TransitionProgressReporter);

            return default;
        }

        protected virtual void OnExit() { }

        internal void AfterExit(Memory<object> args)
        {
            foreach (var lifecycleEvent in _lifecycleEvents)
            {
                lifecycleEvent.DidExit(args);
            }

            gameObject.SetActive(false);
        }

        internal async UniTask BeforeReleaseAsync(Memory<object> args)
        {
            var tasks = _lifecycleEvents.Select(x => x.Cleanup(args));
            await WaitForAsync(tasks);
        }

        protected void SetTransitionProgress(float progress)
        {
            TransitionAnimationProgress = progress;
            TransitionAnimationProgressChanged?.Invoke(progress);
        }

        protected virtual ITransitionAnimation GetAnimation(bool enter, Control partner)
        {
            var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
            return _animationContainer.GetAnimation(enter, partnerIdentifier);
        }
    }
}