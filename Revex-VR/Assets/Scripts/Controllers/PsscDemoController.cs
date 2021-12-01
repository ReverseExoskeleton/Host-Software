using System;
using System.Collections.Generic;
using UnityEngine;

public enum PsscDeviceStatus {
  Asleep,
  Connecting,
  ArmEstimation,
}

public class PsscDemoController : MonoBehaviour {
  // --------------- Scene ---------------
  private PsscDeviceStatus _status = PsscDeviceStatus.Connecting;
  private PsscSimulation _sim;

  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public MovingAvg elbowEma = new MovingAvg();


  void Start() {
    _sim = GameObject.Find("Demo_Objects").GetComponent<PsscSimulation>();
    tranceiver = new BleTranceiver();
    fusion = new Madgwick();
  }

  void Update() {
    PsscDeviceStatus initialStatus = _status;
    switch (_status) {
      case PsscDeviceStatus.Asleep:
        if (tranceiver.WasSleepStatusChanged()) 
          _status = PsscDeviceStatus.ArmEstimation;
        break;
      case PsscDeviceStatus.Connecting:
        if (tranceiver?.TryEstablishConnection() == true)
          _status = PsscDeviceStatus.ArmEstimation;
        break;
      case PsscDeviceStatus.ArmEstimation:
        if (tranceiver.WasSleepStatusChanged()) {
          _status = PsscDeviceStatus.Asleep;
          _timeSinceLastPacketS = 0;
          break;
        }

        if (!UpdateSensorData()) return;
        UpdateTransforms();
        tranceiver?.SendHapticFeedback(GetHapticFeedback());
        break;
      default:
        throw new Exception($"Unknown case {_status}.");
    }
    if (initialStatus != _status) {
      _sim.DisplayStatus(_status);
    }
  }

  private bool UpdateSensorData() {
    _timeSinceLastPacketS += Time.deltaTime;

    List<SensorSample> samples = new List<SensorSample>();
    if (tranceiver?.TryGetSensorData(out samples) == false) return false;
    Debug.Log("After try get sensor data");
    float samplePeriod = _timeSinceLastPacketS / samples.Count;
    _timeSinceLastPacketS = 0;

    foreach (SensorSample sample in samples) {
      fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                        sample.Imu.MagField, samplePeriod);

      elbowEma.Update(sample.ElbowAngleDeg);
    }

    return true;
  }

  private void UpdateTransforms() {
    _sim.DisplayIMUQuat(fusion.GetQuaternion());
    _sim.DisplayElbowAngle(elbowEma.Current());
  }

  private HapticFeedback GetHapticFeedback() {
    HapticFeedbackPercents feedback = _sim.GetHapticDutyCycleAndFreq();
    return new HapticFeedback(feedback.DutyCycle, feedback.Frequency);
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

