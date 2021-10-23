using UnityEngine;
using UnityEngine.Assertions;
using System.Threading;

public enum Scenes {
  VALDISPLAY, CUBE, COMPASS
}

public class Controller : MonoBehaviour {
  public SerialReader reader;
  public Madgwick fusion;
  public ValueDisplay accelDisplay;
  public ValueDisplay gyroDisplay;
  public ValueDisplay magDisplay;
  public Transform tf;
  public Scenes scene;
  private bool _firstUpdate = true;
  private float _timeSinceLastPacketS = 0; // sec

  void Start() {
    try {
      reader = new SerialReader();
    } catch (HardwareConfigurationException) {
      throw;
    }
    if (scene != Scenes.VALDISPLAY) {
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
      switch (scene) {
        case Scenes.VALDISPLAY: {
          accelDisplay.UpdateValue(sample.LinAccel);
          gyroDisplay.UpdateValue(sample.AngVel);
          magDisplay.UpdateValue(sample.MagField);
          break;
        }
        case Scenes.CUBE: {
          fusion.AhrsUpdate(sample.AngVel, sample.LinAccel, 
                        sample.MagField, _timeSinceLastPacketS);
          _timeSinceLastPacketS = 0;

          var q = fusion.GetQuaternion();
          tf.rotation = new Quaternion(q.x, q.z, q.y, q.w);
          Debug.LogWarning($"x={q.x}, y={q.z}, z={q.y}, w={q.w}");
          break;
        }
        case Scenes.COMPASS: {
          tf.rotation = Quaternion.Euler(0, 0, fusion.CompassUpdate(
              sample.LinAccel, sample.MagField));
          break;
        }
        default: throw new System.Exception();
      }
    } catch (PacketQueueEmptyException e) {
      Debug.Log(e);
    }
  }

  private void OnApplicationQuit() {
    if (reader != null) reader.StopReading();
  }

}
