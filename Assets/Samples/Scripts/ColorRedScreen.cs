using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core.Views;

namespace Demo.Scripts
{
    public class ColorRedScreen : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        [SerializeField] private Button _button;

        public override UniTask Initialize(Memory<object> args)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(GoToBlueScreen);

            return UniTask.CompletedTask;
        }

        private void GoToBlueScreen()
        {
            var options = new ViewOptions(ResourceKey.ColorBlueScreenPrefab(), true, loadAsync: false);
            ScreenContainer.Of(transform).Push(options);
        }
    }
}
