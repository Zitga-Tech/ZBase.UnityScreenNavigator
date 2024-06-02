using System;
using UnityEngine;
using UnityEngine.Serialization;
using ZBase.UnityScreenNavigator.Core.Activities;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core.Windows;

namespace ZBase.UnityScreenNavigator.Core
{
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class UnityScreenNavigatorLauncher : WindowContainerManager
    {
        [SerializeField]
        private UnityScreenNavigatorSettings unityScreenNavigatorSettings;

        [SerializeField, FormerlySerializedAs("containerLayerSettings")]
        private WindowContainerSettings windowContainerSettings;

        public UnityScreenNavigatorSettings UnityScreenNavigatorSettings => unityScreenNavigatorSettings;

        public WindowContainerSettings WindowContainerSettings => windowContainerSettings;

        protected sealed override void Awake()
        {
            if (unityScreenNavigatorSettings == false)
            {
                throw new NullReferenceException(nameof(unityScreenNavigatorSettings));
            }

            if (windowContainerSettings == false)
            {
                throw new NullReferenceException(nameof(windowContainerSettings));
            }

            UnityScreenNavigatorSettings.DefaultSettings = unityScreenNavigatorSettings;
            OnAwake();
        }

        protected sealed override void Start()
        {
            OnPreCreateContainers();

            var configs = windowContainerSettings.Containers.Span;

            foreach (var config in configs)
            {
                switch (config.containerType)
                {
                    case WindowContainerType.Modal:
                    {
                        var container = ModalContainer.Create(config, this, unityScreenNavigatorSettings);
                        OnCreateContainer(config, container);
                        break;
                    }

                    case WindowContainerType.Screen:
                    {
                        var container = ScreenContainer.Create(config, this, unityScreenNavigatorSettings);
                        OnCreateContainer(config, container);
                        break;
                    }

                    case WindowContainerType.Activity:
                    {
                        var container = ActivityContainer.Create(config, this, unityScreenNavigatorSettings);
                        OnCreateContainer(config, container);
                        break;
                    }
                }
            }

            OnPostCreateContainers();
        }

        protected virtual void OnAwake() { }

        protected virtual void OnPreCreateContainers() { }

        protected virtual void OnPostCreateContainers() { }

        protected virtual void OnCreateContainer(WindowContainerConfig config, WindowContainerBase container) { }
    }
}