using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
  public OVRInput.Button switchSceneButton = OVRInput.Button.One;
  void Update()
  {
    // The user may still be holding the button, let's give some time to release
    // before consider it a valid input
    if (Time.timeSinceLevelLoad <= 1.0f)
      return;

    if (OVRInput.GetUp(switchSceneButton))
    {
      int sceneIndex = SceneManager.GetActiveScene().buildIndex;
      sceneIndex++;
      if (sceneIndex >= SceneManager.sceneCountInBuildSettings)
      {
        sceneIndex = 0;
      }

      SceneManager.LoadSceneAsync(sceneIndex);

      // Let's avoid triggering multiple scene loads
      this.enabled = false;
    }
  }
}
