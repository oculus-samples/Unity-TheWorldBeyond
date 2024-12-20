using System.Collections.Generic;

namespace UnityEngine.AI
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(-101)]
    [AddComponentMenu("Navigation/NavMeshLink", 33)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshLink : MonoBehaviour
    {
        [SerializeField]
        private int m_AgentTypeID;
        public int agentTypeID { get => m_AgentTypeID; set { m_AgentTypeID = value; UpdateLink(); } }

        [SerializeField]
        private Vector3 m_StartPoint = new(0.0f, 0.0f, -2.5f);
        public Vector3 startPoint { get => m_StartPoint; set { m_StartPoint = value; UpdateLink(); } }

        [SerializeField]
        private Vector3 m_EndPoint = new(0.0f, 0.0f, 2.5f);
        public Vector3 endPoint { get => m_EndPoint; set { m_EndPoint = value; UpdateLink(); } }

        [SerializeField]
        private float m_Width;
        public float width { get => m_Width; set { m_Width = value; UpdateLink(); } }

        [SerializeField]
        private int m_CostModifier = -1;
        public int costModifier { get => m_CostModifier; set { m_CostModifier = value; UpdateLink(); } }

        [SerializeField]
        private bool m_Bidirectional = true;
        public bool bidirectional { get => m_Bidirectional; set { m_Bidirectional = value; UpdateLink(); } }

        [SerializeField]
        private bool m_AutoUpdatePosition;
        public bool autoUpdate { get => m_AutoUpdatePosition; set => SetAutoUpdate(value); }

        [SerializeField]
        private int m_Area;
        public int area { get => m_Area; set { m_Area = value; UpdateLink(); } }

        private NavMeshLinkInstance m_LinkInstance = new();
        private Vector3 m_LastPosition = Vector3.zero;
        private Quaternion m_LastRotation = Quaternion.identity;
        private static readonly List<NavMeshLink> s_Tracked = new();

        private void OnEnable()
        {
            AddLink();
            if (m_AutoUpdatePosition && m_LinkInstance.valid)
                AddTracking(this);
        }

        private void OnDisable()
        {
            RemoveTracking(this);
            m_LinkInstance.Remove();
        }

        public void UpdateLink()
        {
            m_LinkInstance.Remove();
            AddLink();
        }

        private static void AddTracking(NavMeshLink link)
        {
#if UNITY_EDITOR
            if (s_Tracked.Contains(link))
            {
                Debug.LogError("Link is already tracked: " + link);
                return;
            }
#endif

            if (s_Tracked.Count == 0)
                NavMesh.onPreUpdate += UpdateTrackedInstances;

            s_Tracked.Add(link);
        }

        private static void RemoveTracking(NavMeshLink link)
        {
            _ = s_Tracked.Remove(link);

            if (s_Tracked.Count == 0)
                NavMesh.onPreUpdate -= UpdateTrackedInstances;
        }

        private void SetAutoUpdate(bool value)
        {
            if (m_AutoUpdatePosition == value)
                return;
            m_AutoUpdatePosition = value;
            if (value)
                AddTracking(this);
            else
                RemoveTracking(this);
        }

        private void AddLink()
        {
#if UNITY_EDITOR
            if (m_LinkInstance.valid)
            {
                Debug.LogError("Link is already added: " + this);
                return;
            }
#endif

            var link = new NavMeshLinkData
            {
                startPosition = m_StartPoint,
                endPosition = m_EndPoint,
                width = m_Width,
                costModifier = m_CostModifier,
                bidirectional = m_Bidirectional,
                area = m_Area,
                agentTypeID = m_AgentTypeID
            };
            m_LinkInstance = NavMesh.AddLink(link, transform.position, transform.rotation);
            if (m_LinkInstance.valid)
                m_LinkInstance.owner = this;

            m_LastPosition = transform.position;
            m_LastRotation = transform.rotation;
        }

        private bool HasTransformChanged()
        {
            if (m_LastPosition != transform.position) return true;
            return m_LastRotation != transform.rotation;
        }

        private void OnDidApplyAnimationProperties()
        {
            UpdateLink();
        }

        private static void UpdateTrackedInstances()
        {
            foreach (var instance in s_Tracked)
            {
                if (instance.HasTransformChanged())
                    instance.UpdateLink();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_Width = Mathf.Max(0.0f, m_Width);

            if (!m_LinkInstance.valid)
                return;

            UpdateLink();

            if (!m_AutoUpdatePosition)
            {
                RemoveTracking(this);
            }
            else if (!s_Tracked.Contains(this))
            {
                AddTracking(this);
            }
        }
#endif
    }
}
