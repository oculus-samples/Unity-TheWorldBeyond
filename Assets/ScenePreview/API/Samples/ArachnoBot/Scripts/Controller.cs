using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
  [SerializeField] private Transform target;
  [SerializeField] private float linearSpeed = 0.6f; // in m/s
  [SerializeField] private float angularSpeed = 60.0f; // in degrees/s

  // Update is called once per frame
  void Update()
  {
    Vector2 rightThumbstick = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);

    // Move
    target.localPosition += target.forward * rightThumbstick.y * linearSpeed * Time.deltaTime;

    // Rotate
    target.localRotation = Quaternion.AngleAxis(rightThumbstick.x * angularSpeed * Time.deltaTime, target.up) * target.localRotation;
  }
}
