using System;
using System.Collections.Generic;
using UnityEngine;

public enum DeviceStatus {
  Connecting,
  ArmEstimation,
  Asleep,
}

public class PsscDemoController : MonoBehaviour {
  // --------------- Scene ---------------
  private DeviceStatus _status = DeviceStatus.Asleep;
  private HapticFeedback _prevHapticFeedback = new HapticFeedback(-1, -1);
  private PsscSimulation _sim;

  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  public bool useBleTranceiver = true;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public MovingAvg elbowEma = new MovingAvg();
  private Quaternion _ShoulderToImu;
  private Quaternion _bias = Quaternion.identity;

  void Start() {
    _sim = GameObject.Find("Demo_Objects").GetComponent<PsscSimulation>();
    _ShoulderToImu = Quaternion.RotateTowards(_sim.shoulderTf.rotation,
                                              _sim.imuTf.rotation,
                                              float.MaxValue);

    if (useBleTranceiver) {
      tranceiver = new BleTranceiver();
    } else {
      tranceiver = new SerialReader();
    }
    fusion = new Madgwick(Quaternion.identity);
  }

  void Update() {
    if (tranceiver == null) return;

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
        Recalibrate();
        UpdateTransforms();

        HapticFeedback feedback = GetHapticFeedback();
        if (feedback != _prevHapticFeedback) {
          tranceiver.SendHapticFeedback(feedback);
          _prevHapticFeedback = feedback;
        }
        break;
      default:
        throw new Exception($"Unknown case {_status}.");
    }
    if (initialStatus != _status) {
      Logger.Debug($"Status changed from {initialStatus} to {_status}.");
      _sim.DisplayStatus(_status);
    }
  }

  private bool UpdateSensorData() {
    _timeSinceLastPacketS += Time.deltaTime;

    if (!tranceiver.TryGetSensorData(out List<SensorSample> samples)) return false;
    float samplePeriod = _timeSinceLastPacketS / samples.Count;
    _timeSinceLastPacketS = 0;

    foreach (SensorSample sample in samples) {
      fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                        sample.Imu.MagField, samplePeriod);

      elbowEma.Update(sample.ElbowAngleDeg);
    }

    return true;
  }

  private void Recalibrate() {
    bool shouldRecalibrate = Input.GetKeyUp(KeyCode.C);
    if (!shouldRecalibrate) return;
    Logger.Warning("Calibrating");

    Quaternion desiredShoulderTf = Quaternion.Euler(0, 0, -90);
    Quaternion desiredImuTf = desiredShoulderTf * _ShoulderToImu;
    // bias += desired - actual
    _bias *= desiredImuTf * Quaternion.Inverse(fusion.GetQuaternion());
  }

  private void UpdateTransforms() {
    _sim.DisplayIMUQuat(fusion.GetQuaternion());
    //_sim.DisplayIMUQuat(fusion.GetQuaternion() *
    //                    _bias *
    //                    Quaternion.Inverse(_ShoulderToImu));
    _sim.DisplayElbowAngle(elbowEma.Current());
  }

  private HapticFeedback GetHapticFeedback() {
    HapticFeedbackPercents prcnt = _sim.GetHapticDutyCycleAndFreq();
    HapticFeedback feedback = new HapticFeedback(prcnt.DutyCycle, 
                                                 prcnt.Frequency);
    _sim.DisplayHapticFeedbackValues(feedback.Frequency,
                                     feedback.DutyCycle);
    return feedback;
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

