// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

[System.Serializable]
public class WorldBeyondToy : MonoBehaviour
{
    [HideInInspector]
    public bool _isActivated = false;

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
        _isActivated = true;
    }

    public virtual void Deactivate()
    {
        _isActivated = false;
    }
}
