// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using System;
using UnityEngine;

namespace Modules.Teleporter {
  public class TeleportTelegraph : MonoBehaviour {
    [SerializeField] private Renderer _telegraphRenderer;

    public Renderer Renderer => _telegraphRenderer;

    private void Awake() {
      _telegraphRenderer.enabled = false;
    }

    private void OnEnable() {

    }

    private void OnDisable() {
      _telegraphRenderer.enabled = false;
    }
  }
}
