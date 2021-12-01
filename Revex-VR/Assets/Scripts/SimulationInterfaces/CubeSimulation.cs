using UnityEngine;

public class CubeSimulation : MonoBehaviour {
  [SerializeField]
  protected Transform cubeTf;

  public void SetCubeRotation(Quaternion rotation) {
    cubeTf.rotation = rotation;
  }
}
