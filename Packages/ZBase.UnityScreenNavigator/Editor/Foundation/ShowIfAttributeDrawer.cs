using UnityEngine;
using UnityEditor;
using ZBase.UnityScreenNavigator.Foundation;
using System.Diagnostics.CodeAnalysis;

namespace ZBase.UnityScreenNavigator.Editor.Foundation
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
    public class ShowIfAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hideAttribute = (ShowIfAttribute)attribute;
            var enabled = GetHideIfAttributeResult(hideAttribute, property);

            var wasEnabled = GUI.enabled;
            GUI.enabled = enabled;

            if (!hideAttribute.HideInInspector || enabled)
            {
                if (PropertyDrawerFinder.TryFind(property, out var drawer))
                {
                    drawer.OnGUI(position, property, label);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hideAttribute = (ShowIfAttribute)attribute;
            var enabled = GetHideIfAttributeResult(hideAttribute, property);

            if (!hideAttribute.HideInInspector || enabled)
            {
                if (PropertyDrawerFinder.TryFind(property, out var drawer))
                {
                    return drawer.GetPropertyHeight(property, label);
                }
                else
                {
                    return EditorGUI.GetPropertyHeight(property, label);
                }
            }
            else
            {
                // The property is not being drawn
                // We want to undo the spacing added before and after the property
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private bool GetHideIfAttributeResult(ShowIfAttribute hideAttribute, SerializedProperty property)
        {
            var enabled = true;

            // returns the property path of the property we want to apply the attribute to
            var propertyPath = property.propertyPath;
            string conditionPath;

            if (!string.IsNullOrEmpty(hideAttribute.ConditionField))
            {
                // Get the full relative property path of the sourcefield so we can have nested hiding
                // changes the path to the conditionalsource property path
                conditionPath = propertyPath.Replace(property.name, hideAttribute.ConditionField);
                var sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);
                
                if (sourcePropertyValue != null)
                {
                    enabled = CheckPropertyType(sourcePropertyValue);
                }
            }

            if (!string.IsNullOrEmpty(hideAttribute.ConditionField2))
            {
                // changes the path to the conditionalsource property path
                conditionPath = propertyPath.Replace(property.name, hideAttribute.ConditionField2);
                var sourcePropertyValue2 = property.serializedObject.FindProperty(conditionPath);

                if (sourcePropertyValue2 != null)
                {
                    enabled = enabled && CheckPropertyType(sourcePropertyValue2);
                }
            }

            if (hideAttribute.Inverse)
            {
                enabled = !enabled;
            }

            return enabled;
        }

        private bool CheckPropertyType(SerializedProperty sourcePropertyValue)
        {
            switch (sourcePropertyValue.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sourcePropertyValue.boolValue;

                case SerializedPropertyType.ObjectReference:
                    return sourcePropertyValue.objectReferenceValue == true;

                default:
                    ErrorIfNotSupported(sourcePropertyValue);
                    return true;
            }
        }

        [HideInCallstack, DoesNotReturn]
        private static void ErrorIfNotSupported(SerializedProperty sourcePropertyValue)
        {
            Debug.LogError($"Data type of the property used for conditional hiding [{sourcePropertyValue.propertyType}] is currently not supported");
        }
    }
}
