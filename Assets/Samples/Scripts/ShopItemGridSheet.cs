using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Core.Sheets;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace Demo.Scripts
{
    public class ShopItemGridSheet : Sheet
    {
        [SerializeField] private Image _thumbnailImage;
        [SerializeField] private Button _firstThumbButton;

        private int _characterId;

        public void Setup(int index, int characterId)
        {
            Identifier = $"{nameof(ShopItemGridSheet)}{index}";
            _characterId = characterId;
            SetupTransitionAnimations(index);
        }

        public override UniTask Initialize(Memory<Arg> args)
        {
            var key = ResourceKey.CharacterThumbnailSprite(_characterId, 1);
            _thumbnailImage.sprite = DemoAssetLoader.AssetLoader.Load<Sprite>(key).Result;
            _firstThumbButton.onClick.RemoveListener(OnFirstThumbButtonClicked);
            _firstThumbButton.onClick.AddListener(OnFirstThumbButtonClicked);
            return UniTask.CompletedTask;
        }

        private void SetupTransitionAnimations(int index)
        {
            string beforeSheetIdentifierRegex;
            if (index == 0)
            {
                beforeSheetIdentifierRegex = string.Empty;
            }
            else if (index == 1)
            {
                beforeSheetIdentifierRegex = $"{nameof(ShopItemGridSheet)}0";
            }
            else
            {
                beforeSheetIdentifierRegex = $"{nameof(ShopItemGridSheet)}[0-{index - 1}]";
            }

            var afterSheetIdentifierRegex = $"{nameof(ShopItemGridSheet)}[{index + 1}-9]";
            var toLeftExitAnim = SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Center,
                afterAlignment: SheetAlignment.Left, beforeAlpha: 1.0f, afterAlpha: 0.0f);
            var toRightExitAnim = SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Center,
                afterAlignment: SheetAlignment.Right, beforeAlpha: 1.0f, afterAlpha: 0.0f);
            var fromRightEnterAnim = SimpleTransitionAnimationObject.CreateInstance(
                beforeAlignment: SheetAlignment.Right,
                afterAlignment: SheetAlignment.Center, beforeAlpha: 0.0f, afterAlpha: 1.0f);
            var fromLeftEnterAnim = SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Left,
                afterAlignment: SheetAlignment.Center, beforeAlpha: 0.0f, afterAlpha: 1.0f);

            if (!string.IsNullOrEmpty(beforeSheetIdentifierRegex))
            {
                var enterAnimation1 = new SheetTransitionAnimationContainer.TransitionAnimation();
                enterAnimation1.PartnerSheetIdentifierRegex = beforeSheetIdentifierRegex;
                enterAnimation1.AssetType = AnimationAssetType.ScriptableObject;
                enterAnimation1.AnimationObject = fromRightEnterAnim;
                AnimationContainer.EnterAnimations.Add(enterAnimation1);
            }

            var enterAnimation2 = new SheetTransitionAnimationContainer.TransitionAnimation();
            enterAnimation2.PartnerSheetIdentifierRegex = afterSheetIdentifierRegex;
            enterAnimation2.AssetType = AnimationAssetType.ScriptableObject;
            enterAnimation2.AnimationObject = fromLeftEnterAnim;
            AnimationContainer.EnterAnimations.Add(enterAnimation2);

            if (!string.IsNullOrEmpty(beforeSheetIdentifierRegex))
            {
                var exitAnimation1 = new SheetTransitionAnimationContainer.TransitionAnimation();
                exitAnimation1.PartnerSheetIdentifierRegex = beforeSheetIdentifierRegex;
                exitAnimation1.AssetType = AnimationAssetType.ScriptableObject;
                exitAnimation1.AnimationObject = toRightExitAnim;
                AnimationContainer.ExitAnimations.Add(exitAnimation1);
            }

            var exitAnimation2 = new SheetTransitionAnimationContainer.TransitionAnimation();
            exitAnimation2.PartnerSheetIdentifierRegex = afterSheetIdentifierRegex;
            exitAnimation2.AssetType = AnimationAssetType.ScriptableObject;
            exitAnimation2.AnimationObject = toLeftExitAnim;
            AnimationContainer.ExitAnimations.Add(exitAnimation2);
        }

        public override UniTask Cleanup()
        {
            _firstThumbButton.onClick.RemoveListener(OnFirstThumbButtonClicked);
            return UniTask.CompletedTask;
        }

        private void OnFirstThumbButtonClicked()
        {
            var modalContainer = ModalContainer.Find(ContainerKey.Modals);
            var options = new WindowOptions(
                  ResourceKey.CharacterModalPrefab()
                , playAnimation: true
                , poolingPolicy: PoolingPolicy.DisablePooling
                , onLoaded: (modal, args) => {
                    var characterModal = (CharacterModal) modal;
                    characterModal.Setup(_characterId);
                }
            );

            modalContainer.Push(options);
        }
    }
}