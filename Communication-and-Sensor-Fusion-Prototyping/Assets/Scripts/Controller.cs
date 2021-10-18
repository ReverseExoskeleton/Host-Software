using UnityEngine;
using UnityEngine.Assertions;
using System.Threading;

public class Controller : MonoBehaviour {
  public SerialReader Reader;
  public Madgwick Fusion;
  public ValueDisplay AccelDisplay;
  public ValueDisplay GyroDisplay;
  public ValueDisplay MagDisplay;
  private bool _firstUpdate = true;

  void Start() {
    //try {
    //  Reader = new SerialReader();
    //} catch(HardwareConfigurationException) {
    //  throw;
    //}
    //Reader.WaitUntilReady();

    // ------------------ Debugging new madgwick stuff -----------------------
    //Madgwick.Impl.FusionVector3 a = new Madgwick.Impl.FusionVector3();
    //a.x = 6;
    //Debug.Log(a.x);
    //Debug.Log(a.y);
    Madgwick.Impl.FusionBias bias = new Madgwick.Impl.FusionBias();
    Debug.Log(bias.filterCoefficient);
    Madgwick.Impl.FusionBiasInitialise(out bias, 1, 1);
    Debug.Log(bias.filterCoefficient);

  }

  void Update() {
    //if (Reader == null) return;
    //if (_firstUpdate) {
    //  Debug.Log("Update tid =" + Thread.CurrentThread.ManagedThreadId);
    //  _firstUpdate = false;
    //}

    //try {
    //  ImuSample sample = Reader.GetImuSamples();
    //  AccelDisplay.UpdateValue(sample.LinAccel);
    //  GyroDisplay.UpdateValue(sample.AngVel);
    //  MagDisplay.UpdateValue(sample.MagField);
    //} catch (PacketQueueEmptyException e) {
    //  Debug.Log(e);
    //}
  }

}
