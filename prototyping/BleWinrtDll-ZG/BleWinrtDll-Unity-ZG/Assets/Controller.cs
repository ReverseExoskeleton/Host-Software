using UnityEngine;
using System.Threading;

public class Controller : MonoBehaviour {
  public SerialReader reader;
  public Madgwick fusion;
  public Transform tf;
  private bool _firstUpdate = true;
  private float _timeSinceLastPacketS = 0; // sec

  void Start() {
    try {
      reader = new SerialReader();
    } catch (HardwareConfigurationException) {
      throw;
    }
    fusion = new Madgwick();
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
      ImuSample sample = reader.GetImuSamples();
      fusion.AhrsUpdate(sample.AngVel, sample.LinAccel,
                    sample.MagField, _timeSinceLastPacketS);
      _timeSinceLastPacketS = 0;

      Madgwick.Impl.FusionQuaternion q = fusion.GetQuaternion();
      tf.rotation = new Quaternion(q.x, q.z, q.y, q.w);
      Madgwick.Impl.FusionEulerAngles e = fusion.GetEulerAngles();
      Logger.Testing($"roll={e.pitch}, pitch={e.roll}, yaw={e.yaw}");

    } catch (PacketQueueEmptyException e) {
      Logger.Debug(e);
    }
  }

  private void OnApplicationQuit() {
    if (reader != null) reader.StopReading();
  }
}
