using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ModalBackdrop : View
    {
        [SerializeField] private ModalBackdropTransitionAnimationContainer _animationContainer;
        [SerializeField] private bool _closeModalWhenClicked;
        [SerializeField] private Graphic _graphic;

        private float _originalAlpha;

        public ModalBackdropTransitionAnimationContainer AnimationContainer => _animationContainer;

        protected override void Awake()
        {
            SetCloseModalOnClick(_closeModalWhenClicked);

            if (_graphic == false)
            {
                _graphic = GetComponent<Graphic>();
            }

            _originalAlpha = _graphic ? _graphic.color.a : 1f;
        }

        public void Setup(
              RectTransform parent
            , in float? alpha
            , in bool? closeModalWhenClick
        )
        {
            SetCloseModalOnClick(closeModalWhenClick);
            SetAlpha(alpha);

            Parent = parent;
            RectTransform.FillParent(Parent);
            CanvasGroup.interactable = _closeModalWhenClicked;

            gameObject.SetActive(false);
        }

        private void SetAlpha(in float? value)
        {
            if (_graphic == false)
            {
                return;
            }

            var alpha = _originalAlpha;

            if (value.HasValue)
            {
                alpha = value.Value;
            }

            var color = _graphic.color;
            color.a = alpha;
            _graphic.color = color;
        }

        private void SetCloseModalOnClick(in bool? value)
        {
            if (value.HasValue)
            {
                _closeModalWhenClicked = value.Value;
            }

            if (_graphic == false)
            {
                if (TryGetComponent<Graphic>(out var graphic))
                {
                    _graphic = graphic;
                }
                else
                {
                    graphic = gameObject.AddComponent<Image>();
                    graphic.color = Color.clear;
                    _graphic = graphic;
                }
            }

            if (_closeModalWhenClicked)
            {
                if (TryGetComponent<Button>(out var button) == false)
                {
                    button = gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                }

                button.onClick.AddListener(CloseModalOnClick);
            }
            else
            {
                if (TryGetComponent<Button>(out var button))
                {
                    button.onClick.RemoveListener(CloseModalOnClick);
                    Destroy(button);
                }
            }
        }

        private void CloseModalOnClick()
        {
            var modalContainer = ModalContainer.Of(transform);

            if (modalContainer.IsInTransition)
                return;

            modalContainer.Pop(true);
        }

        internal async UniTask<StubEnter> EnterAsync(bool playAnimation)
        {
            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            CanvasGroup.alpha = 1f;

            if (playAnimation)
            {
                var anim = GetAnimation(true);
                anim.Setup(RectTransform);

                await anim.PlayAsync();
            }

            RectTransform.FillParent(Parent);
            return default;
        }

        internal async UniTask<StubEnter> ExitAsync(bool playAnimation)
        {
            gameObject.SetActive(true);
            RectTransform.FillParent(Parent);
            CanvasGroup.alpha = 1f;

            if (playAnimation)
            {
                var anim = GetAnimation(false);
                anim.Setup(RectTransform);

                await anim.PlayAsync();
            }

            CanvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            return default;
        }

        private ITransitionAnimation GetAnimation(bool enter)
        {
            var anim = _animationContainer.GetAnimation(enter);

            if (anim == null)
            {
                return Settings.GetDefaultModalBackdropTransitionAnimation(enter);
            }

            return anim;
        }
    }
}