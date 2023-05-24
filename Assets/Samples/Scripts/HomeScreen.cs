using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Activities;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core.Views;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace Demo.Scripts
{
    public class HomeScreen : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _shopButton;

        public override async UniTask Initialize(Memory<Arg> args)
        {
            _settingButton.onClick.RemoveAllListeners();
            _shopButton.onClick.RemoveAllListeners();

            _settingButton.onClick.AddListener(OnSettingButtonClicked);
            _shopButton.onClick.AddListener(OnShopButtonClicked);

            // Preload the "Shop" page prefab.
            await ScreenContainer.Of(transform).PreloadAsync(ResourceKey.ShopPagePrefab());
            // Simulate loading time.
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }

        public override void DidPushEnter(Memory<Arg> args)
        {
            ActivityContainer.Find(ContainerKey.Activities).Hide(ResourceKey.LoadingActivity());
        }

        public override UniTask Cleanup()
        {
            _settingButton.onClick.RemoveListener(OnSettingButtonClicked);
            _shopButton.onClick.RemoveListener(OnShopButtonClicked);
            ScreenContainer.Of(transform).KeepInPool(ResourceKey.ShopPagePrefab(), 0);
            return UniTask.CompletedTask;
        }

        private void OnSettingButtonClicked()
        {
            var options = new WindowOptions(ResourceKey.SettingsModalPrefab(), true);
            ModalContainer.Find(ContainerKey.Modals).Push(options);
        }

        private void OnShopButtonClicked()
        {
            var options = new WindowOptions(ResourceKey.ShopPagePrefab(), true);
            ScreenContainer.Of(transform).Push(options);
        }
    }
}