using System;
using UnityEngine;
using UnityEngine.UI;

public class PsscSimulation : MonoBehaviour {
  [SerializeField]
  protected Text sleepStatusText;
  [SerializeField]
  protected Text angleText;
  [SerializeField]
  public Transform elbowTf;
  [SerializeField]
  public Transform shoulderTf;
  [SerializeField]
  public Transform imuTf;
  [SerializeField]
  protected Slider freqSlider;
  [SerializeField]
  protected Slider dutySlider;
  [SerializeField]
  protected Text freqText;
  [SerializeField]
  protected Text dutyText;

  private void Start() {
    DisplayElbowAngle(40f); // Display to 40 degrees on start
    DisplayStatus(DeviceStatus.Asleep);
  }

  public void DisplayStatus(DeviceStatus status) {
    switch (status) {
      case DeviceStatus.Asleep:
        sleepStatusText.text = "ASLEEP";
        sleepStatusText.color = Color.red;
        break;
      case DeviceStatus.Connecting:
        sleepStatusText.text = "CONNECTING";
        sleepStatusText.color = Color.yellow;
        break;
      case DeviceStatus.ArmEstimation:
        sleepStatusText.text = "ACTIVE";
        sleepStatusText.color = Color.green;
        break;
      default:
        throw new Exception($"Unknown case {status}.");
    }
  }

  public void DisplayElbowAngle(float angle) {
    angleText.text = angle.ToString("0") + "°";
    elbowTf.localRotation = Quaternion.Euler(0f, 0f, 180 - angle);
  }

  public void DisplayIMUQuat(Quaternion rotation) {
    shoulderTf.rotation = rotation;
  }

  public HapticFeedbackPercents GetHapticDutyCycleAndFreq() {
    return new HapticFeedbackPercents(dutyCycle: dutySlider.value, frequency: freqSlider.value);
  }

  public void DisplayHapticFeedbackValues(int frequency, int dutyCycle) {
    freqText.text = frequency.ToString() + " Hz";
    dutyText.text = dutyCycle.ToString() + " %";
  }
}
