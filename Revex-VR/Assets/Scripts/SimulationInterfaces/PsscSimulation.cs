using System;
using UnityEngine;
using UnityEngine.UI;

public class PsscSimulation : MonoBehaviour {
  [SerializeField]
  protected Text sleepStatusText;
  [SerializeField]
  protected Text angleText;
  [SerializeField]
  protected Transform elbowTf;
  [SerializeField]
  protected Transform shoulderTf;
  [SerializeField]
  protected Transform imuTf;

  private void Start() {
    DisplayElbowAngle(40f); // Display to 40 degrees on start
  }

  public void DisplayStatus(PsscDeviceStatus status) {
    switch (status) {
      case PsscDeviceStatus.Asleep:
        sleepStatusText.text = "ASLEEP";
        sleepStatusText.color = Color.red;
        break;
      case PsscDeviceStatus.Connecting:
        sleepStatusText.text = "CONNECTING";
        sleepStatusText.color = Color.yellow;
        break;
      case PsscDeviceStatus.ArmEstimation:
        sleepStatusText.text = "ACTIVE";
        sleepStatusText.color = Color.green;
        break;
      default:
        throw new Exception($"Unknown case {status}.");
    }
  }

  public void DisplayElbowAngle(float angle) {
    angleText.text = angle.ToString() + "°";
    elbowTf.localRotation = Quaternion.Euler(0f, 0f, angle);
  }

  public void DisplayIMUQuat(Quaternion imuRotation) {
    shoulderTf.rotation = imuRotation * Quaternion.Inverse(imuTf.localRotation);
  }

  public HapticFeedbackPercents GetHapticDutyCycleAndFreq() {
    return new HapticFeedbackPercents(dutyCycle: 0, frequency: 0);
  }
}
