using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core;
using ZBase.UnityScreenNavigator.Core.Views;
using Cysharp.Threading.Tasks;
using ZBase.UnityScreenNavigator.Core.Windows;

namespace Demo.Scripts
{
    public class BasicLauncher : UnityScreenNavigatorLauncher
    {
        public static WindowContainerManager ContainerManager { get; private set; }

        protected override void OnAwake()
        {
            ContainerManager = this;
        }

        protected override void OnPostCreateContainers()
        {
            ShowTopPage().Forget();
        }

        private async UniTaskVoid ShowTopPage()
        {
            var options = new ViewOptions(ResourceKey.ColorRedScreenPrefab(), true, loadAsync: false);
            await ContainerManager.Find<ScreenContainer>(ContainerKey.Screens).PushAsync(options);
        }
    }
}
