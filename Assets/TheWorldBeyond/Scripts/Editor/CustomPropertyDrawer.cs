// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.Utils;
using UnityEditor;
using UnityEngine;

namespace TheWorldBeyond.Editor
{
    [CustomPropertyDrawer(typeof(NamedArrayAttribute))]
    public class NamedArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            try
            {
                var pos = int.Parse(property.propertyPath.Split('[', ']')[1]);
                EditorGUI.ObjectField(rect, property, new GUIContent(((NamedArrayAttribute)attribute).Names[pos]));
            }
            catch
            {
                EditorGUI.ObjectField(rect, property, label);
            }
        }
    }
}
