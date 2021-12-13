using System;
using System.Collections.Generic;
using UnityEngine;

public class CubeDemoController : MonoBehaviour {
  // --------------- Communication ---------------
  private DeviceStatus _status = DeviceStatus.Asleep;
  public Tranceiver tranceiver;
  public bool useBleTranceiver = true;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  //public Mahony fusion;
  //private float startTime;
  //private bool biasSet = false;
  //private Quaternion bias = Quaternion.identity;
  //private Quaternion desired;
  public Transform cubeTf;

  void Start() {
    if (useBleTranceiver) {
      tranceiver = new BleTranceiver();
    } else {
      tranceiver = new SerialReader();
    }
    fusion = new Madgwick();
    //fusion = new Mahony(0.01f, 1f);

    //startTime = Time.time;
    //desired = cubeTf.rotation;
  }

  void Update() {
    if (tranceiver == null) return;

    if (Input.GetKeyDown(KeyCode.Space)) {
      Logger.Warning("Correcting for bias");
      //biasSet = true;
      //bias = Quaternion.Inverse(fusion.GetQuaternion());
      Quaternion bias_deg = Quaternion.Euler(0, 90, 0);
      fusion = new Madgwick();
    }

    DeviceStatus initialStatus = _status;
    switch (_status) {
      case DeviceStatus.Asleep:
        if (tranceiver.DeviceIsAwake(forceDeviceSearch: true)) 
          _status = DeviceStatus.Connecting;
        break;
      case DeviceStatus.Connecting:
        if (tranceiver.TryEstablishConnection())
          _status = DeviceStatus.ArmEstimation;
        break;
      case DeviceStatus.ArmEstimation:
        if (!tranceiver.DeviceIsAwake(forceDeviceSearch: false)) {
          _status = DeviceStatus.Asleep;
          _timeSinceLastPacketS = 0;
          break;
        }

        if (!UpdateSensorData()) return;
        UpdateTransforms();

        break;
      default:
        throw new Exception($"Unknown case {_status}.");
    }

    if (initialStatus != _status) {
      Logger.Debug($"Status changed from {initialStatus} to {_status}.");
    }
  }

  private bool UpdateSensorData() {
    _timeSinceLastPacketS += Time.deltaTime;

    List<SensorSample> samples = new List<SensorSample>();
    if (tranceiver?.TryGetSensorData(out samples) == false) return false;
    float samplePeriod = _timeSinceLastPacketS / samples.Count;
    //fusion.SamplePeriod = samplePeriod;
    _timeSinceLastPacketS = 0;

    foreach (SensorSample sample in samples) {
      fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                        sample.Imu.MagField, samplePeriod);
      //fusion.Update(sample.Imu.AngVel.x * Mathf.Deg2Rad,
      //  sample.Imu.AngVel.y * Mathf.Deg2Rad, sample.Imu.AngVel.z * Mathf.Deg2Rad,
      //        sample.Imu.LinAccel.x, sample.Imu.LinAccel.y, sample.Imu.LinAccel.z,
      //        sample.Imu.MagField.x + 26.025f, sample.Imu.MagField.y - 12.825f,
      //        sample.Imu.MagField.z - 12.825f);
    }

    return true;
  }

  private void UpdateTransforms() {
    cubeTf.rotation = fusion.GetQuaternion();//* bias;
    //cubeTf.rotation *= Quaternion.AngleAxis(-90f, cubeTf.forward);
    Vector3 eulerAng = fusion.GetEulerAngles();
    //float[] q = fusion.Quaternion;
    //cubeTf.rotation = new Quaternion(q[3], q[0], q[1], q[2]) * Quaternion.Inverse(bias);
    Logger.Testing($"roll={eulerAng.z}, pitch={eulerAng.x}, yaw={eulerAng.y}");
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

