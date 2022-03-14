using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor (typeof (ResizableObjectsList))]

public class ResizableObjectsListEditor : Editor {

    private SerializedProperty m_objectsClassification;
    private SerializedProperty m_objectsData;
    private ReorderableList m_ObjectsList;

    private void OnEnable () {
        m_objectsClassification = serializedObject.FindProperty ("classification");
        m_objectsData = serializedObject.FindProperty ("objects");
        m_ObjectsList = new ReorderableList (serializedObject: serializedObject, elements: m_objectsData, draggable: true, displayHeader: true,
            displayAddButton: true, displayRemoveButton: true);
        m_ObjectsList.drawHeaderCallback = DrawHeaderCallback;
        m_ObjectsList.drawElementCallback = DrawElementCallback;
        m_ObjectsList.elementHeightCallback += ElementHeightCallback;
        m_ObjectsList.onAddCallback += OnAddCallback;
    }

    private void DrawHeaderCallback (Rect rect) {
        EditorGUI.LabelField (rect, "Objects");
    }

    private void DrawElementCallback (Rect rect, int index, bool isactive, bool isfocused) {

        SerializedProperty element = m_ObjectsList.serializedProperty.GetArrayElementAtIndex (index);
        rect.y += 2;

        SerializedProperty elementName = element.FindPropertyRelative ("objectName");
        string elementTitle = string.IsNullOrEmpty (elementName.stringValue) ?
            "New Object" : $"{elementName.stringValue}";

        EditorGUI.PropertyField (position:
            new Rect (rect.x += 10, rect.y, Screen.width * .8f, height : EditorGUIUtility.singleLineHeight), property:
            element, label : new GUIContent (elementTitle), includeChildren : true);
    }

    private float ElementHeightCallback (int index) {
        float propertyHeight =
            EditorGUI.GetPropertyHeight (m_ObjectsList.serializedProperty.GetArrayElementAtIndex (index), true);
        float spacing = EditorGUIUtility.singleLineHeight / 2;
        return propertyHeight + spacing;
    }

    private void OnAddCallback (ReorderableList list) {
        var index = list.serializedProperty.arraySize;
        list.serializedProperty.arraySize++;
        list.index = index;
        var element = list.serializedProperty.GetArrayElementAtIndex (index);
    }

    public override void OnInspectorGUI () {
        serializedObject.Update ();
        EditorGUILayout.PropertyField(m_objectsClassification);
        EditorGUILayout.Space();
        m_ObjectsList.DoLayoutList ();
        serializedObject.ApplyModifiedProperties ();
    }
}
