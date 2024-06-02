using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ZBase.UnityScreenNavigator.Foundation;
using System.Diagnostics.CodeAnalysis;

namespace ZBase.UnityScreenNavigator.Editor.Foundation
{
    [CustomPropertyDrawer(typeof(SortingLayerId), true)]
    public class SortingLayerIdDrawer : PropertyDrawer
    {
        private readonly List<GUIContent> layerNames = new();
        private readonly List<int> layerValues = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureLayersInitialized(this.layerValues, this.layerNames);

            var valueProperty = property.FindPropertyRelative(nameof(SortingLayerId.id));

            if (valueProperty == null)
            {
                WarningIfValuePropertyNull();
                return;
            }

            var index = this.layerValues.IndexOf(valueProperty.intValue);

            if (index < 0)
            {
                if (Application.isPlaying)
                {
                    // If application is playing we dont want to change the layers on the fly
                    // Instead, just display them as an int value
                    valueProperty.intValue = EditorGUI.IntField(position, property.displayName, valueProperty.intValue);
                    return;
                }
                else
                {
                    // If the application is not running, reset the layer to the default layer
                    valueProperty.intValue = 0;
                    index = 0;
                }
            }

            var tooltipAttribute = this.fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true)
                .Cast<TooltipAttribute>()
                .FirstOrDefault();

            if (tooltipAttribute != null)
            {
                label.tooltip = tooltipAttribute.tooltip;
            }

            index = EditorGUI.Popup(position, label, index, this.layerNames.ToArray());
            valueProperty.intValue = this.layerValues[index];
        }

        private void EnsureLayersInitialized(List<int> layerValues, List<GUIContent> layerNames)
        {
            layerValues.Clear();
            layerNames.Clear();

            foreach (var layer in SortingLayer.layers)
            {
                layerValues.Add(layer.id);
                layerNames.Add(new GUIContent(layer.name));
            }
        }

        [HideInCallstack, DoesNotReturn]
        private static void WarningIfValuePropertyNull()
        {
            Debug.LogWarning("Could not find the layer index property, was it renamed or removed?");
        }
    }
}
