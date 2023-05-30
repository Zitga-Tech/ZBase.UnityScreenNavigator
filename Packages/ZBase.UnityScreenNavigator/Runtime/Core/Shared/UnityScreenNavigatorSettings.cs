using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core
{
    public sealed partial class UnityScreenNavigatorSettings : ScriptableObject
    {
        private const string DEFAULT_MODAL_BACKDROP_PREFAB_KEY = "DefaultModalBackdrop";

        private static UnityScreenNavigatorSettings _default;

        /// <summary>
        /// The default settings for UnityScreenNavigator.
        /// </summary>
        /// <remarks>
        /// Its value must be manually set before the UnityScreenNavigator is used.
        /// </remarks>
        public static UnityScreenNavigatorSettings DefaultSettings
        {
            get
            {
                ThrowIfDefaultSettingsNull();
                return _default;
            }

            set => _default = value;
        }

        private static void ThrowIfDefaultSettingsNull()
        {
            const string MESSAGE = "UnityScreenNavigatorSettings.DefaultSettings is null. "
                + "Its value must be set before the UnityScreenNavigator is used.";

            if (_default == false)
            {
#if UNITY_EDITOR
                Debug.LogError(MESSAGE);
#else
                throw new System.NullReferenceException(MESSAGE);
#endif
            }
        }

        [SerializeField] private TransitionAnimationObject _sheetEnterAnimation;

        [SerializeField] private TransitionAnimationObject _sheetExitAnimation;

        [SerializeField] private TransitionAnimationObject _screenPushEnterAnimation;

        [SerializeField] private TransitionAnimationObject _screenPushExitAnimation;

        [SerializeField] private TransitionAnimationObject _screenPopEnterAnimation;

        [SerializeField] private TransitionAnimationObject _screenPopExitAnimation;

        [SerializeField] private TransitionAnimationObject _modalEnterAnimation;

        [SerializeField] private TransitionAnimationObject _modalExitAnimation;

        [SerializeField] private TransitionAnimationObject _modalBackdropEnterAnimation;

        [SerializeField] private TransitionAnimationObject _modalBackdropExitAnimation;

        [SerializeField] private TransitionAnimationObject _activityEnterAnimation;

        [SerializeField] private TransitionAnimationObject _activityExitAnimation;

        [SerializeField] private ModalBackdrop _modalBackdropPrefab;

        [SerializeField] private AssetLoaderObject _assetLoader;

        [SerializeField] private bool _enablePooling;

        [SerializeField] private bool _enableInteractionInTransition;

        [SerializeField] private bool _disableModalBackdrop;

        [EnabledIf(nameof(_disableModalBackdrop), false)]
        [SerializeField] private string _modalBackdropResourcePath = DEFAULT_MODAL_BACKDROP_PREFAB_KEY;

        private IAssetLoader _defaultAssetLoader;

        public ITransitionAnimation SheetEnterAnimation => _sheetEnterAnimation
            ? Instantiate(_sheetEnterAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeAlpha: 0.0f, easeType: EaseType.Linear);

        public ITransitionAnimation SheetExitAnimation => _sheetExitAnimation
            ? Instantiate(_sheetExitAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(afterAlpha: 0.0f, easeType: EaseType.Linear);

        public ITransitionAnimation ScreenPushEnterAnimation => _screenPushEnterAnimation
            ? Instantiate(_screenPushEnterAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Right,
                afterAlignment: SheetAlignment.Center);

        public ITransitionAnimation ScreenPushExitAnimation => _screenPushExitAnimation
            ? Instantiate(_screenPushExitAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Center,
                afterAlignment: SheetAlignment.Left);

        public ITransitionAnimation ScreenPopEnterAnimation => _screenPopEnterAnimation
            ? Instantiate(_screenPopEnterAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Left,
                afterAlignment: SheetAlignment.Center);

        public ITransitionAnimation ScreenPopExitAnimation => _screenPopExitAnimation
            ? Instantiate(_screenPopExitAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Center,
                afterAlignment: SheetAlignment.Right);

        public ITransitionAnimation ModalEnterAnimation => _modalEnterAnimation
            ? Instantiate(_modalEnterAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeScale: Vector3.one * 0.3f, beforeAlpha: 0.0f);

        public ITransitionAnimation ModalExitAnimation => _modalExitAnimation
            ? Instantiate(_modalExitAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(afterScale: Vector3.one * 0.3f, afterAlpha: 0.0f);

        public ITransitionAnimation ModalBackdropEnterAnimation => _modalBackdropEnterAnimation
            ? Instantiate(_modalBackdropEnterAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeAlpha: 0.0f, easeType: EaseType.Linear);

        public ITransitionAnimation ModalBackdropExitAnimation => _modalBackdropExitAnimation
            ? Instantiate(_modalBackdropExitAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(afterAlpha: 0.0f, easeType: EaseType.Linear);

        public ITransitionAnimation ActivityEnterAnimation => _activityEnterAnimation
            ? Instantiate(_activityEnterAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(beforeScale: Vector3.one * 0.3f, beforeAlpha: 0.0f);

        public ITransitionAnimation ActivityExitAnimation => _activityExitAnimation
            ? Instantiate(_activityExitAnimation)
            : SimpleTransitionAnimationObject.CreateInstance(afterScale: Vector3.one * 0.3f, afterAlpha: 0.0f);

        public string ModalBackdropResourcePath
        {
            get => string.IsNullOrWhiteSpace(_modalBackdropResourcePath)
                ? DEFAULT_MODAL_BACKDROP_PREFAB_KEY
                : _modalBackdropResourcePath;
        }

        public IAssetLoader AssetLoader
        {
            get
            {
                if (_assetLoader)
                {
                    return _assetLoader;
                }

                if (_defaultAssetLoader == null)
                {
#if USN_USE_ADDRESSABLES
                    _defaultAssetLoader = CreateInstance<AddressableAssetLoaderObject>();
#else
                    _defaultAssetLoader = CreateInstance<ResourcesAssetLoaderObject>();
#endif
                }

                return _defaultAssetLoader;
            }
        }

        public bool EnablePooling => _enablePooling;

        public bool EnableInteractionInTransition => _enableInteractionInTransition;

        public bool DisableModalBackdrop => _disableModalBackdrop;


        public ITransitionAnimation GetDefaultScreenTransitionAnimation(bool push, bool enter)
        {
            if (push)
            {
                return enter ? ScreenPushEnterAnimation : ScreenPushExitAnimation;
            }

            return enter ? ScreenPopEnterAnimation : ScreenPopExitAnimation;
        }

        public ITransitionAnimation GetDefaultModalTransitionAnimation(bool enter)
        {
            return enter ? ModalEnterAnimation : ModalExitAnimation;
        }

        public ITransitionAnimation GetDefaultModalBackdropTransitionAnimation(bool enter)
        {
            return enter ? ModalBackdropEnterAnimation : ModalBackdropExitAnimation;
        }

        public ITransitionAnimation GetDefaultSheetTransitionAnimation(bool enter)
        {
            return enter ? SheetEnterAnimation : SheetExitAnimation;
        }

        public ITransitionAnimation GetDefaultActivityTransitionAnimation(bool enter)
        {
            return enter ? ActivityEnterAnimation : ActivityExitAnimation;
        }
    }
}