using System;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public delegate void OnLoadCallback(Window window, Memory<Arg> args);

    public readonly struct WindowOptions
    {
        public readonly bool loadAsync;
        public readonly bool playAnimation;
        public readonly PoolingPolicy poolingPolicy;
        public readonly string resourcePath;
        public readonly OnLoadCallback onLoaded;

        public WindowOptions(
            string resourcePath
            , bool playAnimation = true
            , OnLoadCallback onLoaded = null
            , bool loadAsync = true
            , PoolingPolicy poolingPolicy = PoolingPolicy.UseSettings
        )
        {
            this.loadAsync = loadAsync;
            this.playAnimation = playAnimation;
            this.poolingPolicy = poolingPolicy;
            this.resourcePath = resourcePath;
            this.onLoaded = onLoaded;
        }

        public static implicit operator WindowOptions(string resourcePath)
            => new(resourcePath);
    }
}