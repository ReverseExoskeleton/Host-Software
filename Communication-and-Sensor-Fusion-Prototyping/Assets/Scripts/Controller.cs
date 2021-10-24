using UnityEngine;
using UnityEngine.Assertions;
using System.Threading;
using AHRS;

public enum Scenes {
  VALDISPLAY, CUBE, COMPASS
}

public class Controller : MonoBehaviour {
  public SerialReader reader;
  public xio_Fusion.FusionAhrs fusion;
  public xio_Fusion.Fusion fusionInterface;
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
      fusionInterface = new xio_Fusion.Fusion();
      fusion = fusionInterface.ahrs;
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
                        //fusion.Update(sample.AngVel.x * Mathf.Deg2Rad, sample.AngVel.y * Mathf.Deg2Rad, sample.AngVel.z * Mathf.Deg2Rad,
                        //              sample.LinAccel.x, sample.LinAccel.y, sample.LinAccel.z,
                        //              sample.MagField.x, sample.MagField.y, sample.MagField.z);
          Vector3 angVel = new Vector3(sample.AngVel.x, sample.AngVel.y, sample.AngVel.z);
          Vector3 linAcl = new Vector3(sample.LinAccel.x, sample.LinAccel.y, sample.LinAccel.z);
          Vector3 magFld = new Vector3(sample.MagField.x, sample.MagField.y, sample.MagField.z);
          
          fusionInterface.FusionAhrsRawUpdate(fusion, angVel, linAcl, magFld, Time.deltaTime);
          _timeSinceLastPacketS = 0;

          var q = fusion.quaternion;
          tf.rotation = new Quaternion(q[0], q[2], q[1], q[3]);
          // Debug.LogWarning($"x={q.x}, y={q.z}, z={q.y}, w={q.w}");
          break;
        }
        case Scenes.COMPASS: {
          //tf.rotation = Quaternion.Euler(0, 0, fusion.CompassUpdate(
          //    sample.LinAccel, sample.MagField));
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
