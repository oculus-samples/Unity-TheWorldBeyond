// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.Toy
{
    [System.Serializable]
    public class WorldBeyondToy : MonoBehaviour
    {
        [HideInInspector]
        public bool IsActivated = false;

        public virtual void Initialize()
        {

        }

        public virtual void ActionDown()
        {

        }

        public virtual void Action()
        {

        }

        public virtual void ActionUp()
        {

        }

        public virtual void Activate()
        {
            IsActivated = true;
        }

        public virtual void Deactivate()
        {
            IsActivated = false;
        }
    }
}
