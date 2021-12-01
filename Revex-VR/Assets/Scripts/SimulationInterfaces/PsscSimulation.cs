using UnityEngine;
using UnityEngine.UI;

public class PsscSimulation : MonoBehaviour {
  public enum deviceSleepStatus {
    asleep,
    awake
  }

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

  public void DisplayStatus(deviceSleepStatus status) {
    if (status == deviceSleepStatus.awake) {
      sleepStatusText.text = "AWAKE";
      sleepStatusText.color = Color.green;
    } else {
      sleepStatusText.text = "ASLEEP";
      sleepStatusText.color = Color.red;
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
