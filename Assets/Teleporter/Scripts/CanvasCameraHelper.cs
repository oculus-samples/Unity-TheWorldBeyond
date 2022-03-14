using UnityEngine;
using UnityEngine.UI;


public class CanvasCameraHelper : MonoBehaviour
{
  [SerializeField] Canvas loadingCanvas;
  [SerializeField] float canvasPlaneDistance = 0.4f;
  [SerializeField] Image backgroundImage;

  private void Awake() {
    Debug.Assert(loadingCanvas != null);
    Debug.Assert(backgroundImage != null);
  }

  private void Start() {
    // setup loading canvas camera
    var centerEye = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().centerEyeAnchor;
    var cam = centerEye.GetComponent<Camera>();
    loadingCanvas.worldCamera = cam;
    loadingCanvas.planeDistance = canvasPlaneDistance;

    // enable background image (diabled per default)
    backgroundImage.enabled = true;
  }
}
