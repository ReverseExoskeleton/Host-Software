using System;
using System.Collections.Generic;
using UnityEngine;


public class MadgwickDemoController : MonoBehaviour {
  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public Transform cubeTf;

  void Start() {
    tranceiver = new SerialReader();
    fusion = new Madgwick();
    tranceiver.EstablishConnection();
  }

  void Update() {
    if (!UpdateSensorData()) return;
    UpdateTransforms();
  }

  private bool UpdateSensorData() {
    _timeSinceLastPacketS += Time.deltaTime;

    List<SensorSample> samples = new List<SensorSample>();
    if (tranceiver?.TryGetSensorData(out samples) == false) return false;
    float samplePeriod = _timeSinceLastPacketS / samples.Count;
    _timeSinceLastPacketS = 0;

    foreach (SensorSample sample in samples) {
      fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                        sample.Imu.MagField, samplePeriod);
    }

    return true;
  }

  private void UpdateTransforms() {
    cubeTf.rotation = fusion.GetQuaternion();
    Vector3 eulerAng = fusion.GetEulerAngles();
    Logger.Testing($"roll={eulerAng.z}, pitch={eulerAng.x}, yaw={eulerAng.y}");
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

