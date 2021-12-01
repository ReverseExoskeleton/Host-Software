using System.Collections.Generic;
using UnityEngine;

public class PsscDemoController : MonoBehaviour {
  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  private float _timeSinceLastPacketS = 0; // sec
  bool connected = false;

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public MovingAvg elbowEma = new MovingAvg();

  // --------------- Scene ---------------
  private PsscSimulation _sim;

  void Start() {
    _sim = GameObject.Find("Demo_Objects").GetComponent<PsscSimulation>();
    tranceiver = new BleTranceiver();
    fusion = new Madgwick();
  }

  void Update() {
    if (!connected) {
      connected = tranceiver.TryEstablishConnection();
    } else {
      if (!UpdateSensorData()) return;
      UpdateTransforms();
      tranceiver.SendHapticFeedback(GetHapticFeedback());
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

  private void UpdateTransforms() {
    _sim.DisplayIMUQuat(fusion.GetQuaternion());
    Vector3 eulerAng = fusion.GetEulerAngles();
    Logger.Testing($"IMU: roll={eulerAng.z}, pitch={eulerAng.x}, yaw={eulerAng.y}");

    _sim.DisplayElbowAngle(elbowEma.Current());
    Logger.Testing($"Elbow Angle (EMA) = {elbowEma.Current()}");
  }

  private HapticFeedback GetHapticFeedback() {
    HapticFeedbackPercents feedback = _sim.GetHapticDutyCycleAndFreq();
    return new HapticFeedback(feedback.DutyCycle, feedback.Frequency);
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

