using UnityEngine;

public class Controller : MonoBehaviour {
  public SerialReader Reader;
  //private bool _readerIsValid = true;

  // Start is called before the first frame update
  void Start() {
    Reader = new SerialReader();
  }

  // Update is called once per frame
  void Update() {
    if (Reader == null) return;
    ImuSample[] samples = Reader.GetImuSamples();
  }
}
