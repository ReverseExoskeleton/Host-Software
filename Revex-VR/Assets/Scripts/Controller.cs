using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class Controller : MonoBehaviour {
  // --------------- Communication ---------------
  public Tranceiver tranceiver;

  private bool _firstUpdate = true;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public Transform cubeTf;

  // -------------- Haptic Feedback --------------

  void Start() {
    tranceiver = new SerialReader();
    fusion = new Madgwick();
    tranceiver.EstablishConnection();
  }

  void Update() {
    if (tranceiver == null) return;
    if (_firstUpdate) {
      Logger.Debug("Update tid =" + Thread.CurrentThread.ManagedThreadId);
      _firstUpdate = false;
    }
    _timeSinceLastPacketS += Time.deltaTime;

    List<SensorSample> samples;
    if (!tranceiver.TryGetSensorData(out samples)) return;
    float samplePeriod = _timeSinceLastPacketS / samples.Count;
    _timeSinceLastPacketS = 0;

    foreach (SensorSample sample in samples) {
      fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                        sample.Imu.MagField, samplePeriod);
    }
    cubeTf.rotation = fusion.GetQuaternion();
    Vector3 eulerAng = fusion.GetEulerAngles();
    Logger.Testing($"roll={eulerAng.z}, pitch={eulerAng.x}, yaw={eulerAng.y}");
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}
