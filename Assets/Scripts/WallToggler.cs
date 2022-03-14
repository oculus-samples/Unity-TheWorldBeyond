using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallToggler : MonoBehaviour
{
    public GameObject _beam;
    MeshRenderer _beamMesh;
    public SceneEnvironment _sceneEnv;

    // Start is called before the first frame update
    void Start()
    {
        _beamMesh = _beam.GetComponentInChildren<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        transform.rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

        Vector3 impactPos = transform.position + transform.forward;
        SanctuaryRoomObject targetedWall = CheckForWall(ref impactPos);
        bool wallCanBeToggled = false;

        if (targetedWall)
        {
            _beam.transform.localScale = new Vector3(1, 1, (transform.position - impactPos).magnitude);
            wallCanBeToggled = targetedWall.CanBeToggled();

            if (OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger))
            {
                targetedWall.ToggleWall(impactPos);
            }
        }

        _beam.gameObject.SetActive(OVRInput.Get(OVRInput.RawButton.RIndexTrigger));
        _beamMesh.material.SetColor("_Color", wallCanBeToggled ? Color.green : Color.red);
    }

    SanctuaryRoomObject CheckForWall(ref Vector3 _hoveredPoint)
    {
        // highlight selected wall
        SanctuaryRoomObject hoveringWall = null;
        Vector3 controllerPos = Vector3.zero;
        Quaternion controllerRot = Quaternion.identity;

        LayerMask acceptableLayers = LayerMask.GetMask("RoomBox", "Furniture");
        RaycastHit[] roomboxHit = Physics.RaycastAll(transform.position, transform.forward, 1000.0f, acceptableLayers);
        float closestHit = 100.0f;
        foreach (RaycastHit hit in roomboxHit)
        {
            GameObject hitObj = hit.collider.gameObject;
            float thisHit = Vector3.Distance(hit.point, controllerPos);
            if (thisHit < closestHit)
            {
                closestHit = thisHit;
                _hoveredPoint = hit.point;
                SanctuaryRoomObject rbs = hitObj.GetComponent<SanctuaryRoomObject>();
                if (rbs)
                {
                    if (rbs.CanBeToggled() && !_sceneEnv.IsFloor(rbs._surfaceID))
                    {
                        hoveringWall = rbs;
                    }
                }
            }
        }

        return hoveringWall;
    }
}
