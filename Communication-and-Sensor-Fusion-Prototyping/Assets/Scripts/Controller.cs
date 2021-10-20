using UnityEngine;
using UnityEngine.Assertions;
using System.Threading;

public class Controller : MonoBehaviour {
  public SerialReader reader;
  public Madgwick fusion;
  public ValueDisplay accelDisplay;
  public ValueDisplay gyroDisplay;
  public ValueDisplay magDisplay;
  public Transform cubeTf;
  public bool isMadgwickDemo = false;
  private bool _firstUpdate = true;
  private float _timeSinceLastPacketS = 0; // sec

  void Start() {
    try {
      reader = new SerialReader();
    } catch (HardwareConfigurationException) {
      throw;
    }
    if (isMadgwickDemo) {
      fusion = new Madgwick();
    }
    reader.WaitUntilReady();
  }

  void Update() {
    if (reader == null) return;
    if (_firstUpdate) {
      Debug.Log("Update tid =" + Thread.CurrentThread.ManagedThreadId);
      _firstUpdate = false;
    }

    _timeSinceLastPacketS += Time.deltaTime;
    try {
      ImuSample sample = reader.GetImuSamples();
      if (isMadgwickDemo) {
        fusion.Update(sample.AngVel, sample.LinAccel, 
                      sample.MagField, _timeSinceLastPacketS);
        _timeSinceLastPacketS = 0;

        var q = fusion.GetQuaternion();
        cubeTf.rotation = new Quaternion(q.x, q.z, q.y, q.w);
        Debug.LogWarning($"x={q.x}, y={q.z}, z={q.y}, w={q.w}");
      } else {
        accelDisplay.UpdateValue(sample.LinAccel);
        gyroDisplay.UpdateValue(sample.AngVel);
        magDisplay.UpdateValue(sample.MagField);
      }
    } catch (PacketQueueEmptyException e) {
      Debug.Log(e);
    }
  }

  private void OnApplicationQuit() {
    if (reader != null) reader.StopReading();
  }

}
