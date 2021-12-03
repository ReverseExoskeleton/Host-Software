﻿using System;
using System.Collections.Generic;
using UnityEngine;

public enum PsscDeviceStatus {
  Connecting,
  ArmEstimation,
  Asleep,
}

public class PsscDemoController : MonoBehaviour {
  // --------------- Scene ---------------
  private PsscDeviceStatus _status = PsscDeviceStatus.Connecting;
  private HapticFeedback _prevHapticFeedback = new HapticFeedback(-1, -1);
  private PsscSimulation _sim;

  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  public bool useBleTranceiver = true;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public MovingAvg elbowEma = new MovingAvg();
  private Quaternion _bias = Quaternion.identity;

  void Start() {
    _sim = GameObject.Find("Demo_Objects").GetComponent<PsscSimulation>();
    if (useBleTranceiver) {
      tranceiver = new BleTranceiver();
    } else {
      tranceiver = new SerialReader();
    }
    fusion = new Madgwick();
  }

  void Update() {
    PsscDeviceStatus initialStatus = _status;
    switch (_status) {
      case PsscDeviceStatus.Asleep:
        if (tranceiver?.DeviceIsAwake() == true) 
          _status = PsscDeviceStatus.ArmEstimation;
        break;
      case PsscDeviceStatus.Connecting:
        if (tranceiver?.TryEstablishConnection() == true)
          _status = PsscDeviceStatus.ArmEstimation;
        break;
      case PsscDeviceStatus.ArmEstimation:
        if (tranceiver?.DeviceIsAwake() == false) {
          _status = PsscDeviceStatus.Asleep;
          _timeSinceLastPacketS = 0;
          break;
        }

        if (!UpdateSensorData()) return;
        Recalibrate();
        UpdateTransforms();
        HapticFeedback feedback = GetHapticFeedback();
        if (feedback != _prevHapticFeedback) {
          tranceiver?.SendHapticFeedback(feedback);
          _prevHapticFeedback = feedback;
        }
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

  private void Recalibrate() {
    bool shouldRecalibrate = Input.GetKeyDown(KeyCode.C);
    if (!shouldRecalibrate) return;
    Logger.Warning("Calibrating");

    Quaternion desiredShoulderTf = Quaternion.Euler(0, 0, -90);
    //Quaternion desiredImuTf = desiredShoulderTf * _sim.imuTf.localRotation;
    // bias += desired - actual
    _bias = Quaternion.RotateTowards(fusion.GetQuaternion(), desiredShoulderTf, float.MaxValue);

    //_bias *= desiredShoulderTf * Quaternion.Inverse(fusion.GetQuaternion());
  }

  private void UpdateTransforms() {
    _sim.DisplayIMUQuat(
        fusion.GetQuaternion() *
        _bias);
        //_bias * 
        //Quaternion.Inverse(_sim.imuTf.localRotation));
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

