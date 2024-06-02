using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Editor.Foundation.AssetLoader
{
    [CustomPropertyDrawer(typeof(LazyAssetLoaderObject.KeyAssetPair))]
    internal sealed class LazyAssetObjectKeyAssetPairPropertyDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, PropertyData> _dataList = new();
        private PropertyData _property;

        private void Init(SerializedProperty property)
        {
            if (_dataList.TryGetValue(property.propertyPath, out _property)) return;

            _property = new PropertyData {
                KeySourceProperty = property.FindPropertyRelative("_keySource"),
                KeyProperty = property.FindPropertyRelative("_key"),
                AssetProperty = property.FindPropertyRelative("_asset"),
            };

            _dataList.Add(property.propertyPath, _property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);
            var fieldRect = position;
            fieldRect.height = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.PropertyScope(fieldRect, label, property))
            {
                property.isExpanded = EditorGUI.Foldout(new Rect(fieldRect), property.isExpanded, label, true);

                if (property.isExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(new Rect(fieldRect), _property.KeySourceProperty);

                        var keySource = (KeySourceType)_property.KeySourceProperty.intValue;

                        if (keySource == KeySourceType.AssetName)
                        {
                            AssignAssetNameToKey(_property);
                        }

                        GUI.enabled = keySource == KeySourceType.InputField;
                        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(new Rect(fieldRect), _property.KeyProperty);
                        GUI.enabled = true;

                        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(new Rect(fieldRect), _property.AssetProperty);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);
            var height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;

            return height;
        }

        private static void AssignAssetNameToKey(PropertyData data)
        {
            var obj = data.AssetProperty.objectReferenceValue;

            if (obj)
            {
                data.KeyProperty.stringValue = obj.name;
            }
        }

        private class PropertyData
        {
            public SerializedProperty AssetProperty;
            public SerializedProperty KeyProperty;
            public SerializedProperty KeySourceProperty;
        }
    }
}