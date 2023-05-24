using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.PriorityCollection;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    [DisallowMultipleComponent]
    public class Activity : Window, IActivityLifecycleEvent
    {
        [SerializeField]
        private ActivityTransitionAnimationContainer _animationContainer = new();

        private readonly UniquePriorityList<IActivityLifecycleEvent> _lifecycleEvents = new();
        private Progress<float> _transitionProgressReporter;

        private Progress<float> TransitionProgressReporter
        {
            get
            {
                return _transitionProgressReporter ??= new Progress<float>(SetTransitionProgress);
            }
        }

        public ActivityTransitionAnimationContainer AnimationContainer => _animationContainer;

        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Return the transition animation type currently playing.
        /// If not in transition, return null.
        /// </summary>
        public ActivityTransitionAnimationType? TransitionAnimationType { get; private set; }

        /// <summary>
        /// Progress of the transition animation.
        /// </summary>
        public float TransitionAnimationProgress { get; private set; }

        /// <summary>
        /// Event when the transition animation progress changes.
        /// </summary>
        public event Action<float> TransitionAnimationProgressChanged;

        public void SetSortingLayer(SortingLayerId? layer, int? sortingOrder)
        {
            if ((layer.HasValue & sortingOrder.HasValue) == false)
            {
                return;
            }

            var canvas = this.GetOrAddComponent<Canvas>();
            var _ = this.GetOrAddComponent<GraphicRaycaster>();

            canvas.overrideSorting = true;

            if (layer.HasValue)
                canvas.sortingLayerID = layer.Value.id;

            if (sortingOrder.HasValue)
                canvas.sortingOrder = sortingOrder.Value;
        }

        /// <inheritdoc/>
        public virtual UniTask Initialize(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual UniTask WillShow(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidShow(Memory<Arg> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask WillHide(Memory<Arg> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidHide(Memory<Arg> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask Cleanup()
        {
            return UniTask.CompletedTask;
        }

        public void AddLifecycleEvent(IActivityLifecycleEvent lifecycleEvent, int priority = 0)
        {
            _lifecycleEvents.Add(lifecycleEvent, priority);
        }

        public void RemoveLifecycleEvent(IActivityLifecycleEvent lifecycleEvent)
        {
            _lifecycleEvents.Remove(lifecycleEvent);
        }

        internal async UniTask AfterLoadAsync(RectTransform parentTransform, Memory<Arg> args)
        {
            _lifecycleEvents.Add(this, 0);
            SetIdentifer();

            Parent = parentTransform;
            RectTransform.FillParent(Parent);
            Alpha = 0.0f;

            var tasks = _lifecycleEvents.Select(x => x.Initialize(args));
            await WaitForAsync(tasks);
        }

        internal async UniTask BeforeEnterAsync(bool show, Memory<Arg> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = show
                ? ActivityTransitionAnimationType.ShowEnter
                : ActivityTransitionAnimationType.HideEnter;

            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            SetTransitionProgress(0.0f);

            Alpha = 0.0f;

            var tasks = show
                ? _lifecycleEvents.Select(x => x.WillShow(args))
                : _lifecycleEvents.Select(x => x.WillHide(args));
            
            await WaitForAsync(tasks);
        }

        internal async UniTask EnterAsync(bool show, bool playAnimation)
        {
            Alpha = 1.0f;

            if (playAnimation)
            {
                var anim = GetAnimation(show);
                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
            }

            RectTransform.FillParent(Parent);
            SetTransitionProgress(1.0f);
        }

        internal void AfterEnter(bool show, Memory<Arg> args)
        {
            if (show)
            {
                foreach (var lifecycleEvent in _lifecycleEvents)
                {
                    lifecycleEvent.DidShow(args);
                }
            }
            else
            {
                foreach (var lifecycleEvent in _lifecycleEvents)
                {
                    lifecycleEvent.DidHide(args);
                }
            }

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

        private ITransitionAnimation GetAnimation(bool enter)
        {
            var anim = _animationContainer.GetAnimation(enter);

            if (anim == null)
            {
                return Settings.GetDefaultActivityTransitionAnimation(enter);
            }

            return anim;
        }
    }
}