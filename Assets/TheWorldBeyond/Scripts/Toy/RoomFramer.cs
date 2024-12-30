// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using TheWorldBeyond.Audio;
using TheWorldBeyond.Environment;
using TheWorldBeyond.Environment.RoomEnvironment;
using TheWorldBeyond.GameManagement;
using UnityEngine;

namespace TheWorldBeyond.Toy
{
    public class RoomFramer : WorldBeyondToy
    {
        public static RoomFramer Instance = null;
        private WorldBeyondRoomObject m_hoveredSurface = null;
        private Vector3 m_hoveredPoint = Vector3.zero;
        private Vector3 m_hoveredNormal = Vector3.up;
        public GameObject SparkPrefab;
        public GameObject HoverBorderPrefab;
        public GameObject WallOpenEffect;
        public GameObject WallCloseEffect;
        private LineRenderer m_pointerLine;
        [HideInInspector]
        public ParticleSystem PointerSparks;
        private ParticleSystemRenderer m_sparksRenderer = null;

        public Color WallHitColor;
        public Color FurnishingHitColor;

        private enum HoveredObject
        {
            None,
            Wall,
            Floor,
            Furnishing
        };

        public override void Initialize()
        {
            if (!Instance)
            {
                Instance = this;
            }

            var pointerObj = Instantiate(HoverBorderPrefab);
            m_pointerLine = pointerObj.GetComponent<LineRenderer>();
            m_pointerLine.gameObject.SetActive(false);

            var sparkObj = Instantiate(SparkPrefab);
            PointerSparks = sparkObj.GetComponent<ParticleSystem>();
            m_sparksRenderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            MultiToy.Instance.WallToyTarget = sparkObj.transform;
        }

        private void Update()
        {
            if (!IsActivated)
            {
                return;
            }

            // highlight selected wall
            var hoveringWall = CheckForWall();
            SetBeamColor(hoveringWall);

            // handle any UI
            MultiToy.Instance.PointingAtWall();

            m_pointerLine.positionCount = 2;
            m_pointerLine.SetPosition(0, MultiToy.Instance.GetMuzzlePosition());
            m_pointerLine.SetPosition(1, m_hoveredPoint);

            PointerSparks.transform.position = m_hoveredPoint;
            PointerSparks.transform.rotation = Quaternion.Lerp(PointerSparks.transform.rotation, Quaternion.LookRotation(-m_hoveredNormal), 0.3f);

            WorldBeyondManager.Instance.AffectDebris(m_hoveredPoint, true);
        }

        public override void ActionUp()
        {
            if (m_hoveredSurface != null)
            {
                var clickedWall = m_hoveredSurface.GetComponent<WorldBeyondRoomObject>();
                var surfacePassthroughActive = clickedWall.ToggleWall(m_hoveredPoint);
                var surfaceId = m_hoveredSurface.SurfaceID;
                VirtualRoom.Instance.CloseWall(surfaceId, surfacePassthroughActive);
                var wallSound = surfacePassthroughActive ? MultiToy.Instance.WallToyWallClose : MultiToy.Instance.WallToyWallOpen;
                AudioManager.Instance.PlayAudio(wallSound, clickedWall.transform.position);

                if (!surfacePassthroughActive)
                {
                    WorldBeyondManager.Instance.OpenedWall(surfaceId);
                    WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.ShootWall);
                }

                // recalculate the outdoor audio Position
                var audioPos = Vector3.up;
                var audioOn = VirtualRoom.Instance.GetOutdoorAudioPosition(ref audioPos);
                WorldBeyondEnvironment.Instance.SetOutdoorAudioParams(audioPos, audioOn);

                _ = StartCoroutine(ExpandWallRing(surfacePassthroughActive));
            }
        }

        private HoveredObject CheckForWall()
        {
            // highlight selected wall
            var hoveringWall = HoveredObject.None;
            var controllerPos = Vector3.zero;
            var controllerRot = Quaternion.identity;
            WorldBeyondManager.Instance.GetDominantHand(ref controllerPos, ref controllerRot);

            // modify the beam direction, for hands
            if (WorldBeyondManager.Instance.UsingHands)
            {
                controllerRot *= Quaternion.Euler(-60, 0, 0);
            }

            LayerMask acceptableLayers = LayerMask.GetMask("RoomBox", "Furniture");
            var roomboxHit = Physics.RaycastAll(controllerPos, controllerRot * Vector3.forward, 1000.0f, acceptableLayers);
            var closestHit = 100.0f;
            var targetPoint = m_hoveredPoint;
            foreach (var hit in roomboxHit)
            {
                var hitObj = hit.collider.gameObject;
                var thisHit = Vector3.Distance(hit.point, controllerPos);
                if (thisHit < closestHit)
                {
                    closestHit = thisHit;
                    targetPoint = hit.point + hit.normal * 0.02f;
                    m_hoveredNormal = hit.normal;
                    var rbs = hitObj.GetComponent<WorldBeyondRoomObject>();
                    if (rbs)
                    {
                        if (rbs.IsFurniture)
                        {
                            hoveringWall = HoveredObject.Furnishing;
                        }
                        else if (VirtualRoom.Instance.IsFloor(rbs.SurfaceID))
                        {
                            hoveringWall = HoveredObject.Floor;
                        }
                        else
                        {
                            if (rbs.CanBeToggled())
                            {
                                m_hoveredSurface = rbs;
                                hoveringWall = HoveredObject.Wall;
                            }
                            else
                            {
                                hoveringWall = HoveredObject.None;
                            }
                        }
                    }
                }
            }

            m_hoveredPoint = Vector3.Lerp(m_hoveredPoint, targetPoint, 0.1f);
            if (hoveringWall != HoveredObject.Wall)
            {
                m_hoveredSurface = null;
            }

            return hoveringWall;
        }

        public override void Activate()
        {
            base.Activate();
            m_hoveredSurface = null;
            m_hoveredPoint = transform.position;
            var surf = CheckForWall();
            if (surf != HoveredObject.None)
            {
                m_pointerLine.gameObject.SetActive(true);
                PointerSparks.transform.position = m_hoveredPoint;
                PointerSparks.transform.rotation = Quaternion.LookRotation(-m_hoveredNormal);
                MultiToy.Instance.WallToyLoop_1.Play();
                SetBeamColor(surf);
            }
            PointerSparks.Play();
        }

        private void SetBeamColor(HoveredObject surf)
        {
            var beamColor = surf == HoveredObject.Wall ? WallHitColor : FurnishingHitColor;
            m_pointerLine.sharedMaterial.SetColor("_Color", beamColor);
            m_sparksRenderer.sharedMaterial.SetColor("_Color", beamColor);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            m_pointerLine.gameObject.SetActive(false);
            PointerSparks.Stop();
            PointerSparks.Clear();
            MultiToy.Instance.WallToyLoop_1.Stop();
        }

        public bool IsHighlightingWall()
        {
            return m_pointerLine ? m_pointerLine.gameObject.activeSelf && m_hoveredSurface != null : false;
        }

        private IEnumerator ExpandWallRing(bool ptActive)
        {
            // caution: the timing and numbers here should match the material effect in PassthroughWall.shader
            var ringRot = Quaternion.LookRotation(m_hoveredNormal);
            var effectObj = ptActive ? Instantiate(WallCloseEffect, m_hoveredPoint, ringRot) : Instantiate(WallOpenEffect, m_hoveredPoint, ringRot);
            var effectPrt = effectObj?.GetComponent<ParticleSystem>();
            var timer = 0.0f;
            var effectTime = 1.0f;
            while (timer <= effectTime)
            {
                timer += Time.deltaTime;
                if (effectPrt)
                {
                    var shape = effectPrt.shape;
                    // it takes 1 second to reach 10m radius
                    shape.radius = timer * 5;

                    var maxParticleRate = 150;
                    var rate = effectPrt.emission;
                    var pingpong = Mathf.Abs(timer - 0.5f) * 2;
                    rate.rateOverTime = maxParticleRate * (1 - pingpong) * 100;
                }
                yield return null;
            }
        }
    }
}
