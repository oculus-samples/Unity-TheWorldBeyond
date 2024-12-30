// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CanEditMultipleObjects]
[CustomEditor(typeof(BoneSim))]
public class BoneSimEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoneSim sim = target as BoneSim;
        DrawDefaultInspector();
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Physics"))
        {
            EditorPrefs.SetFloat("BoneSim_Localize", sim.Localize);
            EditorPrefs.SetFloat("BoneSim_Blend", sim.Blend);
            EditorPrefs.SetFloat("BoneSim_Mass", sim.Mass);
            EditorPrefs.SetFloat("BoneSim_Stiffness", sim.Stiffness);
            EditorPrefs.SetFloat("BoneSim_Damping", sim.Damping);
            EditorPrefs.SetFloat("BoneSim_Gravity", sim.Gravity);
        }
        if (GUILayout.Button("Paste Physics"))
        {
            foreach (BoneSim s in targets)
            {
                s.Localize = EditorPrefs.GetFloat("BoneSim_Localize");
                s.Blend = EditorPrefs.GetFloat("BoneSim_Blend");
                s.Mass = EditorPrefs.GetFloat("BoneSim_Mass");
                s.Stiffness = EditorPrefs.GetFloat("BoneSim_Stiffness");
                s.Damping = EditorPrefs.GetFloat("BoneSim_Damping");
                s.Gravity = EditorPrefs.GetFloat("BoneSim_Gravity");
                EditorUtility.SetDirty(s);
            }
        }
        GUILayout.EndHorizontal();
    }
}
#endif

public class BoneSim : MonoBehaviour
{
    [System.Serializable]
    public class Bone
    {
        public Transform bone;

        [Tooltip("The transform that the bone is aiming toward. If no transform specified an offset position will be assigned")]
        public Transform child;

        public Vector3 ForVec { get; set; }
        public Vector3 UpVec { get; set; }
        public Vector3 TargetPos { get; set; } // local to Space
        public Vector3 SimPos { get; set; } // local to Space
        public Quaternion LocalRotationAxis { get; set; }
        [HideInInspector] public Vector3 Force;
        [HideInInspector] public Vector3 Acc;
        [HideInInspector] public Vector3 Vel;

        [HideInInspector] public Pose StartPose = default;
        public bool StartAcquired { get; set; }
    }

    public Bone[] Chain;

    [Tooltip("Blend simulation on/off")]
    [Range(0, 1)] public float Blend = 1f;

    [Tooltip("Local space transform that the simulation lives in. Usually the root of the character, or whichever transform is moving the skeleton around.")]
    public Transform Space;

    [Tooltip("Blend simulation between world space and local space. Good for toning things down when physics moves the character around.")]
    [Range(0, 1)] public float Localize = 0f;

    [Tooltip("If your sim looks saggy or is going nuts, try this. Mechanim will skip evaluating bones that don't have animation on them, so this resets their position in Update before the sim.")]
    public bool ForceUpdate = false;

    [Header("Physics")]
    [Range(0.001f, 1)] public float Mass = 0.2f;
    [Range(0, 1)] public float Stiffness = 0.3f;
    [Range(0, 1)] public float Damping = 0.3f;
    [Range(-1, 1)] public float Gravity = 0.1f;

    private float m_stiffness;
    private float m_mass;
    private float m_damping;
    private float m_gravity;
    private Vector3 m_animatedUpVec;
    private Vector3 m_spacePrevPos;
    private Quaternion m_spacePrevRot;
    private Vector3 m_spacePosDelta;
    private Quaternion m_spaceRotDelta;

    public bool OrderedEvaluation { get; set; }

    private readonly float m_gizmoSize = 0.025f;

    #region Mono

    private void OnEnable()
    {
        if (!OrderedEvaluation)
        {
            Init();
        }
    }

    private void LateUpdate()
    {
        if (!OrderedEvaluation)
        {
            Tick();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        for (int i = 0; i < Chain.Length; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(Chain[i].bone.position, m_gizmoSize * 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(Space.TransformPoint(Chain[i].TargetPos), Vector3.one * m_gizmoSize * 0.5f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(Space.TransformPoint(Chain[i].SimPos), Vector3.one * m_gizmoSize);

            Gizmos.color = Color.black;
            Gizmos.DrawLine(Chain[i].bone.position, Space.TransformPoint(Chain[i].SimPos));
        }
    }

    #endregion

    #region Sim

    public void Init()
    {
        // get start pose (only do this on first init)
        for (int i = 0; i < Chain.Length; i++)
        {
            if (!Chain[i].StartAcquired)
            {
                Chain[i].StartPose = new Pose(Chain[i].bone.localPosition, Chain[i].bone.localRotation);
                Chain[i].StartAcquired = true;
            }
        }

        // get local space
        if (Space == null)
        {
            Space = GetComponentInParent<Animator>().transform;
        }

        for (int i = 0; i < Chain.Length; i++)
        {
            // assign local forward/up
            Chain[i].ForVec = Chain[i].child != null ?
                Chain[i].bone.InverseTransformVector(Chain[i].child.position - Chain[i].bone.position).normalized :
                Chain[i].bone.InverseTransformVector(Chain[i].bone.position - Chain[i].bone.parent.position).normalized;

            Chain[i].UpVec = UpFromForward(Chain[i].ForVec);
            Chain[i].LocalRotationAxis = Quaternion.Inverse(Quaternion.LookRotation(Chain[i].ForVec, Chain[i].UpVec));

            Chain[i].TargetPos = Chain[i].child != null ?
                Space.InverseTransformPoint(Chain[i].child.position) :
                Space.InverseTransformPoint(Chain[i].bone.TransformPoint(Chain[i].ForVec));

            Chain[i].SimPos = Chain[i].TargetPos;

            // reset
            Chain[i].Force = Vector3.zero;
            Chain[i].Acc = Vector3.zero;
            Chain[i].Vel = Vector3.zero;
            m_spacePrevPos = Space.position;
            m_spacePrevRot = Space.localRotation;
            m_spacePosDelta = Vector3.zero;
            m_spaceRotDelta = Quaternion.identity;
        }
    }

    public void Tick()
    {
        // give sliders nice values
        m_mass = Mass;
        m_stiffness = Mathf.Pow(Stiffness, 1.8f);
        m_damping = Mathf.Pow((1f - Damping) * 0.1f, 1.3f);
        m_gravity = Gravity * 0.01f;

        // get space movement
        m_spacePosDelta = Space.InverseTransformVector(Space.position - m_spacePrevPos);
        m_spaceRotDelta = Space.localRotation * Quaternion.Inverse(m_spacePrevRot);
        m_spacePrevPos = Space.position;
        m_spacePrevRot = Space.localRotation;

        // calculate sim
        for (int i = 0; i < Chain.Length; i++)
        {
            // force update
            if (ForceUpdate)
            {
                Chain[i].bone.localPosition = Chain[i].StartPose.position;
                Chain[i].bone.localRotation = Chain[i].StartPose.rotation;
            }

            // get target
            Chain[i].TargetPos = Chain[i].child != null ?
                Space.InverseTransformPoint(Chain[i].child.position) :
                Space.InverseTransformPoint(Chain[i].bone.TransformPoint(Chain[i].ForVec));

            // animated upvector
            m_animatedUpVec = Chain[i].bone.rotation * Chain[i].UpVec;

            // Calculate force, acceleration, and Velocity per X, Y and Z
            Chain[i].Force.x = (Chain[i].TargetPos.x - Chain[i].SimPos.x) * m_stiffness;
            Chain[i].Acc.x = Chain[i].Force.x / m_mass;
            Chain[i].Vel.x += Chain[i].Acc.x * m_damping;

            Chain[i].Force.y = (Chain[i].TargetPos.y - Chain[i].SimPos.y) * m_stiffness;
            Chain[i].Force.y -= m_gravity; // Add some gravity
            Chain[i].Acc.y = Chain[i].Force.y / m_mass;
            Chain[i].Vel.y += Chain[i].Acc.y * m_damping;

            Chain[i].Force.z = (Chain[i].TargetPos.z - Chain[i].SimPos.z) * m_stiffness;
            Chain[i].Acc.z = Chain[i].Force.z / m_mass;
            Chain[i].Vel.z += Chain[i].Acc.z * m_damping;

            // sim position
            Chain[i].SimPos += Chain[i].Vel + Chain[i].Force;
            Chain[i].SimPos = Vector3.Lerp(Chain[i].SimPos, Quaternion.Inverse(m_spaceRotDelta) * (Chain[i].SimPos - m_spacePosDelta), 1f - Localize);

            // pose bone
            Chain[i].bone.rotation = Quaternion.Lerp(Chain[i].bone.rotation,
                Quaternion.LookRotation(Space.TransformPoint(Chain[i].SimPos) - Chain[i].bone.position, m_animatedUpVec) * Chain[i].LocalRotationAxis,
                Blend);
        }
    }

    #endregion

    #region Helpers

    public static Vector3 UpFromForward(Vector3 direction)
    {
        float max = Mathf.Max(direction.x, direction.y, direction.z);
        if (max == direction.x)
            return Vector3.up;
        else if (max == direction.y)
            return Vector3.forward;
        else if (max == direction.z)
            return Vector3.up;
        return default;
    }

    #endregion
}
