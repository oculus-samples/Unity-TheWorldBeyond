// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace TheWorldBeyond.SamplePrefabs
{
    public class SampleDoorDebris : MonoBehaviour
    {
        private bool m_anchorInitialized = false;
        public GameObject DebrisPrefab;

        // optional: if this exists, reposition it slightly above the ground
        public GameObject FloorFadeObject;

        private void Update()
        {
            if (m_anchorInitialized) return;

            if (!gameObject.GetComponentInParent<MRUKAnchor>()) return;

            var anchor = gameObject.GetComponentInParent<MRUKAnchor>();
            if (anchor.Label != MRUKAnchor.SceneLabels.DOOR_FRAME) return;

            SpawnDebris(anchor.PlaneBoundary2D[0]);
            m_anchorInitialized = true;
        }

        private void SpawnDebris(Vector2 doorDimensions)
        {
            if (DebrisPrefab == null)
            {
                return;
            }

            // get bottom corners of door
            var basePos = transform.position - Vector3.up * doorDimensions.y * 0.5f;
            // move it into room a bit, to minimize object intersection with door frame
            var insetAmount = 0.1f;
            basePos += transform.forward * insetAmount;
            var offset = transform.right * (doorDimensions.x * 0.5f - insetAmount);

            // for this example, only do 2 debris
            // modify this to create your own debris distribution
            for (var i = 0; i < 2; i++)
            {
                var debris = Instantiate(DebrisPrefab);
                // quick remapping of iterator (0,1) to left/right side (-1, +1)
                debris.transform.position = basePos + offset * (i - 0.5f) * 2;
                debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
                debris.transform.parent = transform;
            }
        }

        public void AdjustFloorFade(float floorHeight)
        {
            if (FloorFadeObject)
            {
                var pos = FloorFadeObject.transform.position;
                FloorFadeObject.transform.position = new Vector3(pos.x, floorHeight + 0.01f, pos.z);
            }
        }
    }
}
