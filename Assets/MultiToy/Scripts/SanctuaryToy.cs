// Copyright(c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

[System.Serializable]
public class SanctuaryToy : MonoBehaviour
{
    [HideInInspector]
    public bool _isActivated = false;

    public virtual void Initialize()
    {

    }

    // OVRInput.GetDown
    public virtual void ActionDown()
    {

    }

    // OVRInput.Get
    public virtual void Action()
    {

    }

    // OVRInput.GetDown
    public virtual void ActionUp()
    {

    }

    public virtual void SecondAction()
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
