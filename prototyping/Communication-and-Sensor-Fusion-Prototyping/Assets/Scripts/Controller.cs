using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public enum Scenes {
  VALDISPLAY, CUBE, COMPASS
}

public class Controller : MonoBehaviour {
  public SerialReader reader;
  // --------------------------------------------------------------------------
  // xio C Madgwick Lib with DLL |
  // -----------------------------
  public Madgwick fusion;

  // xio C# Mahony Lib |
  // -------------------
  //public AHRS.MahonyAHRS fusion;

  // C# Re-impl of xio C Madgwick Lib |
  // ----------------------------------
  //public xio_Fusion.FusionAhrs fusion;
  //public xio_Fusion.Fusion fusionInterface;
  // --------------------------------------------------------------------------
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
      // ------------------------------------------------------------
      // xio C Madgwick Lib with DLL |
      // -----------------------------
      fusion = new Madgwick();

      // xio C# Mahony Lib |
      // -------------------
     //fusion = new AHRS.MahonyAHRS(0.01f, 1, 0f);

      // C# Re-impl of xio C Madgwick Lib |
      // ----------------------------------
      //fusionInterface = new xio_Fusion.Fusion();
      //fusion = fusionInterface.ahrs;
      // ------------------------------------------------------------
    }
    reader.WaitUntilReady();
  }

  void Update() {
    if (reader == null) return;
    if (_firstUpdate) {
      Logger.Debug("Update tid =" + Thread.CurrentThread.ManagedThreadId);
      _firstUpdate = false;
    }

    _timeSinceLastPacketS += Time.deltaTime;
    try {
      List<ImuSample> samples = reader.GetImuSamples();
      ImuSample mostRecentSample = samples[samples.Count - 1];
      switch (scene) {
        case Scenes.VALDISPLAY: {
          accelDisplay.UpdateValue(mostRecentSample.LinAccel);
          gyroDisplay.UpdateValue(mostRecentSample.AngVel);
          magDisplay.UpdateValue(mostRecentSample.MagField);
          break;
        }
        case Scenes.CUBE: {
          // ------------------------------------------------------------
          // xio C Madgwick Lib with DLL |
          // -----------------------------
          float samplePeriod = samples.Count > 1 ? 0.01f : _timeSinceLastPacketS; // 10 ms
          _timeSinceLastPacketS = 0;
          foreach (ImuSample sample in samples) {
            fusion.AhrsUpdate(sample.AngVel, sample.LinAccel,
                              sample.MagField, samplePeriod);
          }

          Madgwick.Impl.FusionQuaternion q = fusion.GetQuaternion();
          tf.rotation = new Quaternion(q.x, q.z, q.y, q.w);
          Madgwick.Impl.FusionEulerAngles e = fusion.GetEulerAngles();
          Logger.Testing($"roll={e.pitch}, pitch={e.roll}, yaw={e.yaw}");

          // xio C# Mahony Lib |
          // -------------------
          //fusion.Update(sample.AngVel.x * Mathf.Deg2Rad,
          //                sample.AngVel.y * Mathf.Deg2Rad,
          //                sample.AngVel.z * Mathf.Deg2Rad,
          //              sample.LinAccel.x, sample.LinAccel.y, sample.LinAccel.z,
          //              sample.MagField.x, sample.MagField.y, sample.MagField.z);
          //float[] q = fusion.Quaternion;
          //Quaternion initialQ = new Quaternion(q[0], q[2], q[1], q[3]);
          //Vector3 initialEuler = initialQ.eulerAngles;
          //Vector3 updatedEuler = new Vector3(initialEuler.z, initialEuler.x, initialEuler.y);
          //Logger.Testing($"x={updatedEuler.x}, y={updatedEuler.y}, z={updatedEuler.z}");
          //tf.rotation = Quaternion.Euler(updatedEuler);

          // C# Re-impl of xio C Madgwick Lib |
          // ----------------------------------
          //Vector3 angVel = new Vector3(sample.AngVel.x, sample.AngVel.y, sample.AngVel.z);
          //Vector3 linAcl = new Vector3(sample.LinAccel.x, sample.LinAccel.y, sample.LinAccel.z);
          //Vector3 magFld = new Vector3(sample.MagField.x, sample.MagField.y, sample.MagField.z);
          //fusionInterface.FusionAhrsRawUpdate(fusion, angVel, linAcl, magFld, _timeSinceLastPacketS);
          //_timeSinceLastPacketS = 0;
          //var q = fusion.quaternion;
          //tf.rotation = new Quaternion(q[0], q[2], q[1], q[3]);
          //Logger.Testing($"x={q[0]}, y={q[2]}, z={q[1]}, w={q[3]}");
          // ------------------------------------------------------------
          break;
        }
        case Scenes.COMPASS: {
          // ------------------------------------------------------------
          // xio C Madgwick Lib with DLL |
          // -----------------------------
          //tf.rotation = Quaternion.Euler(0, 0, fusion.CompassUpdate(
          //    mostRecentSample.LinAccel, mostRecentSample.MagField));
          // ------------------------------------------------------------
          break;
        }
        default: throw new System.Exception();
      }
    } catch (PacketQueueEmptyException e) {
      Logger.Debug(e);
    }
  }

  private void OnApplicationQuit() {
    if (reader != null) reader.StopReading();
  }

}
