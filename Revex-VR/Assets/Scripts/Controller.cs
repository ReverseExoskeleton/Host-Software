using System;
using System.Collections.Generic;
using UnityEngine;

enum Status {
  Connecting,
  CalibrateShoulder,
  CalibrateArmLength,
  ArmEstimation,
}


public class Controller : MonoBehaviour {
  private Status _status = Status.Connecting;
  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public MovingAvg elbowEma = new MovingAvg();

  public Transform controllerTf;
  public Transform headsetTf;

  public Transform shoulderTf;
  public Transform imuTf;
  public Transform elbowTf;
  public Transform wristTf;

  // Ratio of upper arm to forearm is 1.2 : 1
  private float _upperArmPercent = 0.545F;

  // -------------- Haptic Feedback --------------

  void Start() {
    tranceiver = new SerialReader();
    fusion = new Madgwick();
    tranceiver.EstablishConnection();
    _status = Status.CalibrateShoulder;
  }

  void Update() {
    if (!UpdateSensorData()) return;

    switch (_status) {
      case Status.CalibrateShoulder:
        ShoulderCalibrate();
        break;
      case Status.CalibrateArmLength:
        ArmLengthCalibrate();
        break;
      case Status.ArmEstimation:
        UpdateTransforms();
        tranceiver.SendHapticFeedback(GetHapticFeedback());
        break;
      default:
        Logger.Error("Unknown case in Controller::Update");
        break;
    }
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

      elbowEma.Update(sample.ElbowAngleDeg);
    }

    return true;
  }

  private void ShoulderCalibrate() {
    bool userInDesiredPosition = false;
    // TODO: Add the things here needed to instruct the user to move their
    //       arm to the desired position. Once they do, set the flag below
    //       to true, otherwise false.
    userInDesiredPosition = Input.GetKeyUp(KeyCode.D);
    // ------------------------------------------------------------------

    if (userInDesiredPosition) {
      shoulderTf.position = new Vector3(
        controllerTf.position.x, controllerTf.position.y, headsetTf.position.z);
      _status = Status.CalibrateArmLength;
    }
  }

  private void ArmLengthCalibrate() {
    bool userInDesiredPosition = false;
    // TODO: Add the things here needed to instruct the user to move their
    //       arm to the desired position. Once they do, set the flag below
    //       to true, otherwise false.
    userInDesiredPosition = Input.GetKeyUp(KeyCode.D);
    // ------------------------------------------------------------------

    if (!userInDesiredPosition) return;

    float headToShoulderDist = Vector3.Distance(
      headsetTf.position, shoulderTf.position);
    float headToControllerDist = Vector3.Distance(
      headsetTf.position, controllerTf.position);
    float armLength = (float)Math.Sqrt(
      Math.Pow(headToShoulderDist, 2) + Math.Pow(headToControllerDist, 2));
    float upperArmLength = armLength * _upperArmPercent;
    float forearmLength = armLength - upperArmLength;

    elbowTf.position = shoulderTf.position + new Vector3(upperArmLength, 0, 0);
    wristTf.position = elbowTf.position + new Vector3(forearmLength, 0, 0);

    _status = Status.ArmEstimation;
  }

  private void UpdateTransforms() {
    shoulderTf.rotation = fusion.GetQuaternion() * 
                          Quaternion.Inverse(imuTf.localRotation);
    Vector3 imuAng = fusion.GetEulerAngles();
    Logger.Testing($"IMU: roll={imuAng.z}, pitch={imuAng.x}, yaw={imuAng.y}");
    Vector3 shoulderAng = shoulderTf.eulerAngles;
    Logger.Testing($@"IMU: roll={shoulderAng.z}, 
                      pitch={shoulderAng.x}, yaw={shoulderAng.y}");

    elbowTf.localEulerAngles = new Vector3(elbowEma.Current(), 0, 0);
    Logger.Testing($"Elbow Angle (EMA) = {elbowEma.Current()}");
  }

  private HapticFeedback GetHapticFeedback() {
    // TODO: Come back to after VR simulation is complete.
    return new HapticFeedback(dutyCyclePercent:0, frequencyPercent:0);
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

