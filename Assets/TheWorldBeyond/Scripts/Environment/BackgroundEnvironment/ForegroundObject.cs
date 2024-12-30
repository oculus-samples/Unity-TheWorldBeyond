// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.Environment
{
    public class ForegroundObject : MonoBehaviour
    {
        // just a script identifier to know this object should be culled from the room
        public Transform ShadowObject;
    }
}
