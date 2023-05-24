using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Core.Sheets;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace Demo.Scripts
{
    public class CharacterModal : Modal
    {
        private const int ImageCount = 3;
        [SerializeField] private SheetContainer _imageContainer;
        [SerializeField] private CharacterModalThumbnailGrid thumbnailGrid;
        [SerializeField] private Button _expandButton;

        public RectTransform CharacterImageRectTransform => (RectTransform) _imageContainer.transform;

        private readonly (int sheetId, CharacterModalImageSheet sheet)[] _imageSheets =
            new (int sheetId, CharacterModalImageSheet sheet)[ImageCount];

        private int _characterId;
        private int _selectedRank;

        public void Setup(int characterId)
        {
            _characterId = characterId;
        }

        public override async UniTask Initialize(Memory<Arg> args)
        {
            var imageContainer = _imageContainer;
            var imageSheets = _imageSheets;

            for (var index = 0; index < ImageCount; index++)
            {
                var options = new SheetOptions(
                    resourcePath: ResourceKey.CharacterModalImageSheetPrefab(),
                    onLoaded: (sheetId, sheet) => {
                        imageSheets[index] = (sheetId, (CharacterModalImageSheet)sheet);
                    }
                );

                await imageContainer.RegisterAsync(options);
            }

            _expandButton.onClick.RemoveListener(OnExpandButtonClicked);
            _expandButton.onClick.AddListener(OnExpandButtonClicked);
        }

        public override async UniTask WillPushEnter(Memory<Arg> args)
        {
            var imageContainer = _imageContainer;
            var imageSheets = _imageSheets;

            for (var i = 0; i < ImageCount; i++)
            {
                imageSheets[i].sheet.Setup(_characterId, i + 1);
            }

            await imageContainer.ShowAsync(imageSheets[0].sheetId, false);

            _selectedRank = 1;

            thumbnailGrid.Setup(_characterId);
            thumbnailGrid.ThumbnailClicked += x =>
            {
                if (imageContainer.IsInTransition)
                {
                    return;
                }

                var targetSheet = imageSheets[x];
                if (imageContainer.ActiveSheet.Equals(targetSheet.sheet))
                {
                    return;
                }

                var sheetId = targetSheet.sheetId;
                imageContainer.Show(sheetId, true);
                _selectedRank = x + 1;
            };
        }

        public override UniTask Cleanup()
        {
            _expandButton.onClick.RemoveListener(OnExpandButtonClicked);
            return UniTask.CompletedTask;
        }

        private void OnExpandButtonClicked()
        {
            var options = new WindowOptions(ResourceKey.CharacterImageModalPrefab(), true,
                onLoaded: (modal, args) =>
                {
                    var characterImageModal = (CharacterImageModal) modal;
                    characterImageModal.Setup(_characterId, _selectedRank);
                });

            ModalContainer.Find(ContainerKey.Modals).Push(options);
        }
    }
}