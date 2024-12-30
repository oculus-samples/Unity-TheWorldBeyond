using UnityEngine.AI;

namespace UnityEditor.AI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshModifier))]
    internal class NavMeshModifierEditor : Editor
    {
        private SerializedProperty m_AffectedAgents;
        private SerializedProperty m_Area;
        private SerializedProperty m_IgnoreFromBuild;
        private SerializedProperty m_OverrideArea;

        private void OnEnable()
        {
            m_AffectedAgents = serializedObject.FindProperty("m_AffectedAgents");
            m_Area = serializedObject.FindProperty("m_Area");
            m_IgnoreFromBuild = serializedObject.FindProperty("m_IgnoreFromBuild");
            m_OverrideArea = serializedObject.FindProperty("m_OverrideArea");

            NavMeshVisualizationSettings.showNavigation++;
        }

        private void OnDisable()
        {
            NavMeshVisualizationSettings.showNavigation--;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _ = EditorGUILayout.PropertyField(m_IgnoreFromBuild);

            _ = EditorGUILayout.PropertyField(m_OverrideArea);
            if (m_OverrideArea.boolValue)
            {
                EditorGUI.indentLevel++;
                NavMeshComponentsGUIUtility.AreaPopup("Area Type", m_Area);
                EditorGUI.indentLevel--;
            }

            NavMeshComponentsGUIUtility.AgentMaskPopup("Affected Agents", m_AffectedAgents);
            EditorGUILayout.Space();

            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
