using UnityEngine;
using UnityEngine.Assertions;
using System.Threading;

public class Controller : MonoBehaviour {
  public SerialReader Reader;
  public ValueDisplay AccelDisplay;
  public ValueDisplay GyroDisplay;
  public ValueDisplay MagDisplay;
  //private bool _first_update = true;

  void Start() {
    try {
      Reader = new SerialReader();
    } catch(HardwareConfigurationException) {
      throw;
    }
    Reader.WaitUntilReady();
  }

  void Update() {
    if (Reader == null) return;
    //if (_first_update) {
    //  Debug.Log("Update tid =" + Thread.CurrentThread.ManagedThreadId);
    //  _first_update = false;
    //}

    try {
      ImuSample sample = Reader.GetImuSamples();
      AccelDisplay.UpdateValue(sample.LinAccel);
      GyroDisplay.UpdateValue(sample.AngVel);
      MagDisplay.UpdateValue(sample.MagField);
    } catch (PacketQueueEmptyException e) {
      Debug.Log(e);
    }
  }

}
