using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Sheets;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace Demo.Scripts
{
    public class ShopScreen : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        private const int ItemGridSheetCount = 3;

        [SerializeField] private SheetContainer _itemGridContainer;
        [SerializeField] private Button[] _itemGridButtons;

        private readonly int[] _itemGridSheetIds = new int[ItemGridSheetCount];

        public override async UniTask Initialize(Memory<Arg> args)
        {
            var itemGridContainer = _itemGridContainer;
            var itemGridSheetIds = _itemGridSheetIds;
            var itemGridButtons = _itemGridButtons;

            for (var i = 0; i < ItemGridSheetCount; i++)
            {
                var index = i;
                var options = new SheetOptions(
                    resourcePath: ResourceKey.ShopItemGridSheetPrefab(),
                    onLoaded: (id, sheet) => OnSheetLoaded(id, sheet, index)
                );

                var sheetId = await itemGridContainer.RegisterAsync(options);
                var button = itemGridButtons[index];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowSheet(sheetId).Forget());
            }

            await itemGridContainer.ShowAsync(itemGridSheetIds[0], false);
        }

        public override void DidPopExit(Memory<Arg> args)
        {
            _itemGridContainer.Deinitialize();
        }

        public override void DidPushExit(Memory<Arg> args)
        {
            _itemGridContainer.Deinitialize();
        }

        private async UniTaskVoid ShowSheet(int sheetId)
        {
            if (_itemGridContainer.IsInTransition)
            {
                return;
            }

            if (_itemGridContainer.ActiveSheetId == sheetId)
            {
                // This sheet is already displayed.
                return;
            }

            await _itemGridContainer.ShowAsync(sheetId, true);
        }

        private void OnSheetLoaded(int sheetId, Sheet sheet, int index)
        {
            _itemGridSheetIds[index] = sheetId;
            var shopItemGrid = (ShopItemGridSheet)sheet;
            shopItemGrid.Setup(index, GetCharacterId(index));
        }

        private int GetCharacterId(int index)
        {
            switch (index)
            {
                case 0:
                    return 3;
                case 1:
                    return 4;
                case 2:
                    return 5;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}