using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

#if ZBASE_FOUNDATION_MVVM
using Arg = ZBase.Foundation.Mvvm.Unions.Union;
#else
using Arg = System.Object;
#endif

namespace Demo.Scripts
{
    public class CharacterImageModal : Modal
    {
        [SerializeField] private Image _image;

        private int _characterId;
        private int _rank;

        public RectTransform ImageTransform => (RectTransform)_image.transform;

        public void Setup(int characterId, int rank)
        {
            _characterId = characterId;
            _rank = rank;
        }
        public override async UniTask WillPushEnter(Memory<Arg> args)
        {
            var resourceKey = ResourceKey.CharacterSprite(_characterId, _rank);
            var handle = DemoAssetLoader.AssetLoader.LoadAsync<Sprite>(resourceKey);
            await handle.Task;
            var sprite = handle.Result;
            _image.sprite = sprite;
        }

    }
}
