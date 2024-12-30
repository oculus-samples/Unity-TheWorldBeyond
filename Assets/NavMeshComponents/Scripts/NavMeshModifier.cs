using System.Collections.Generic;

namespace UnityEngine.AI
{
    [ExecuteInEditMode]
    [AddComponentMenu("Navigation/NavMeshModifier", 32)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshModifier : MonoBehaviour
    {
        [SerializeField]
        private bool m_OverrideArea;
        public bool overrideArea { get => m_OverrideArea; set => m_OverrideArea = value; }

        [SerializeField]
        private int m_Area;
        public int area { get => m_Area; set => m_Area = value; }

        [SerializeField]
        private bool m_IgnoreFromBuild;
        public bool ignoreFromBuild { get => m_IgnoreFromBuild; set => m_IgnoreFromBuild = value; }

        // List of agent types the modifier is applied for.
        // Special values: empty == None, m_AffectedAgents[0] =-1 == All.
        [SerializeField]
        private List<int> m_AffectedAgents = new(new int[] { -1 });    // Default value is All

        public static List<NavMeshModifier> activeModifiers { get; } = new();

        private void OnEnable()
        {
            if (!activeModifiers.Contains(this))
                activeModifiers.Add(this);
        }

        private void OnDisable()
        {
            _ = activeModifiers.Remove(this);
        }

        public bool AffectsAgentType(int agentTypeID)
        {
            if (m_AffectedAgents.Count == 0)
                return false;
            return m_AffectedAgents[0] == -1 ? true : m_AffectedAgents.IndexOf(agentTypeID) != -1;
        }
    }
}
