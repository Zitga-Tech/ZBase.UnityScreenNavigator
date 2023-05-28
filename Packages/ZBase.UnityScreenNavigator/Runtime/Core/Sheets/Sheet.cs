using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.PriorityCollection;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [DisallowMultipleComponent]
    public class Sheet : View, ISheetLifecycleEvent
    {
        [SerializeField]
        private int _renderingOrder;

        [SerializeField]
        private SheetTransitionAnimationContainer _animationContainer = new();

        private readonly UniquePriorityList<ISheetLifecycleEvent> _lifecycleEvents = new();
        private Progress<float> _transitionProgressReporter;

        private Progress<float> TransitionProgressReporter
        {
            get
            {
                if (_transitionProgressReporter == null)
                    _transitionProgressReporter = new Progress<float>(SetTransitionProgress);
                return _transitionProgressReporter;
            }
        }

        public SheetTransitionAnimationContainer AnimationContainer => _animationContainer;

        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Return the transition animation type currently playing.
        /// If not in transition, return null.
        /// </summary>
        public SheetTransitionAnimationType? TransitionAnimationType { get; private set; }

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
        public virtual void Deinitialize(Memory<object> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask Cleanup()
        {
            return UniTask.CompletedTask;
        }

        public void AddLifecycleEvent(ISheetLifecycleEvent lifecycleEvent, int priority = 0)
        {
            _lifecycleEvents.Add(lifecycleEvent, priority);
        }

        public void RemoveLifecycleEvent(ISheetLifecycleEvent lifecycleEvent)
        {
            _lifecycleEvents.Remove(lifecycleEvent);
        }

        internal async UniTask AfterLoadAsync(RectTransform parentTransform, Memory<object> args)
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
                var childPage = child.GetComponent<Sheet>();
                siblingIndex = i;
                if (_renderingOrder >= childPage._renderingOrder)
                {
                    continue;
                }

                break;
            }

            RectTransform.SetSiblingIndex(siblingIndex);
            gameObject.SetActive(false);

            var tasks = _lifecycleEvents.Select(x => x.Initialize(args));
            await WaitForAsync(tasks);
        }

        internal async UniTask BeforeEnterAsync(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = SheetTransitionAnimationType.Enter;
            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            SetTransitionProgress(0.0f);

            Alpha = 0.0f;

            var tasks = _lifecycleEvents.Select(x => x.WillEnter(args));
            await WaitForAsync(tasks);
        }

        internal async UniTask EnterAsync(bool playAnimation, Sheet partnerSheet)
        {
            Alpha = 1.0f;

            if (playAnimation)
            {
                var anim = GetAnimation(true, partnerSheet);

                if (partnerSheet)
                {
                    anim.SetPartner(partnerSheet.RectTransform);
                }

                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
            }

            RectTransform.FillParent(Parent);
        }

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
            TransitionAnimationType = SheetTransitionAnimationType.Exit;
            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            SetTransitionProgress(0.0f);

            Alpha = 1.0f;

            var tasks = _lifecycleEvents.Select(x => x.WillExit(args));
            await WaitForAsync(tasks);
        }

        internal async UniTask ExitAsync(bool playAnimation, Sheet partnerSheet)
        {
            if (playAnimation)
            {
                var anim = GetAnimation(false, partnerSheet);

                if (partnerSheet)
                {
                    anim.SetPartner(partnerSheet.RectTransform);
                }

                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
            }

            Alpha = 0.0f;
            SetTransitionProgress(1.0f);
        }

        internal void AfterExit(Memory<object> args)
        {
            foreach (var lifecycleEvent in _lifecycleEvents)
            {
                lifecycleEvent.DidExit(args);
            }

            gameObject.SetActive(false);
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

        private ITransitionAnimation GetAnimation(bool enter, Sheet partner)
        {
            var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
            var anim = _animationContainer.GetAnimation(enter, partnerIdentifier);

            if (anim == null)
            {
                return Settings.GetDefaultSheetTransitionAnimation(enter);
            }

            return anim;
        }
    }
}