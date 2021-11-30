using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class pssc_demo_interface : MonoBehaviour
{
    public enum deviceSleepStatus
    {
        asleep,
        awake
    }

    [SerializeField]
    protected Text sleepStatusText;
    [SerializeField]
    protected Text angleText;
    [SerializeField]
    protected Transform elbowTF;
    [SerializeField]
    protected Transform shoulderTF;

    private void Start()
    {
        DisplayElbowAngle(40f); // Display to 40 degrees on start
    }

    public void DisplayStatus(deviceSleepStatus status)
    {
        if (status == deviceSleepStatus.awake)
        {
            sleepStatusText.text = "AWAKE";
            sleepStatusText.color = Color.green;
        }
        else
        {
            sleepStatusText.text = "ASLEEP";
            sleepStatusText.color = Color.red;
        }
    }

    public void DisplayElbowAngle(float angle)
    {
        angleText.text = angle.ToString() + "°";
        elbowTF.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void DisplayIMUQuat(Quaternion rotation)
    {
        shoulderTF.rotation = rotation;
    }
}
