using System;
using System.Collections.Generic;
using UnityEngine;

public class NinjaArmController : MonoBehaviour
{
    // --------------- Communication ---------------
    public DeviceStatus status = DeviceStatus.Asleep;
    public Tranceiver tranceiver;
    public bool useBleTranceiver = true;
    private float _timeSinceLastPacketS = 0; // sec

    // --------------- Arm Estimation ---------------
    public Madgwick fusion;
    public MovingAvg elbowEma = new MovingAvg();

    private Vector3 shoulderStart;
    public Transform imu;
    public Transform shoulder;
    public Transform elbow;

    // -------------------- Game --------------------
    public List<GameObject> fruitsHit = new List<GameObject>();

    HapticFeedback _feedback;
    float _hapticBurstEndTime;

    void Start()
    {
        shoulderStart = shoulder.position;

        if (useBleTranceiver)
        {
            tranceiver = new BleTranceiver();
        }
        else
        {
            tranceiver = new SerialReader();
        }
        fusion = new Madgwick();
    }

    void Update()
    {
        if (tranceiver == null) return;

        DeviceStatus initialStatus = status;
        switch (status)
        {
            case DeviceStatus.Asleep:
                if (tranceiver.DeviceIsAwake(forceDeviceSearch: true))
                    status = DeviceStatus.Connecting;
                break;
            case DeviceStatus.Connecting:
                if (tranceiver.TryEstablishConnection())
                    status = DeviceStatus.ArmEstimation;
                break;
            case DeviceStatus.ArmEstimation:
                if (!tranceiver.DeviceIsAwake(forceDeviceSearch: false))
                {
                    status = DeviceStatus.Asleep;
                    _timeSinceLastPacketS = 0;
                    break;
                }

                if (_feedback.DutyCycle != 0)
                {
                    if (Time.time > _hapticBurstEndTime)
                    {
                        _feedback = new HapticFeedback(0, 0);
                        tranceiver.SendHapticFeedback(_feedback);
                    }

                }

                if (!UpdateSensorData()) return;
                UpdateTransforms();


                break;
            default:
                throw new Exception($"Unknown case {status}.");
        }

        if (initialStatus != status)
        {
            Logger.Debug($"Status changed from {initialStatus} to {status}.");
        }
    }

    public void Recalibrate(float yaw) {
        Logger.Debug("Recalibrating");
        fusion.SetYaw(yaw);
    }

  private bool UpdateSensorData()
    {
        _timeSinceLastPacketS += Time.deltaTime;

        List<SensorSample> samples = new List<SensorSample>();
        if (!tranceiver.TryGetSensorData(out samples)) return false;
        float samplePeriod = _timeSinceLastPacketS / samples.Count;
        //fusion.SamplePeriod = samplePeriod;
        _timeSinceLastPacketS = 0;

        foreach (SensorSample sample in samples)
        {
            fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                              sample.Imu.MagField, samplePeriod);
            elbowEma.Update(sample.ElbowAngleDeg);
        }


        return true;
    }

    public void HapticBurst(float burstPeriodS, float dutyCyclePrcnt, float frequencyPrcnt)
    {
        _feedback = new HapticFeedback(dutyCyclePrcnt, frequencyPrcnt);
        tranceiver.SendHapticFeedback(_feedback);
        _hapticBurstEndTime = Time.time + burstPeriodS;
    }

    private void UpdateTransforms()
    {
        imu.rotation = fusion.GetQuaternion();
        imu.position += shoulderStart - shoulder.position;  // Make it rotate around the shoulder

        elbow.localRotation = Quaternion.Euler(0f, 0f, 180f - elbowEma.Current());

        float batteryVoltage = tranceiver.GetLastBatteryVoltage(); // Do something with this
        Logger.Testing($"Battery voltage = {batteryVoltage }V");
    }

    private void OnApplicationQuit()
    {
        tranceiver?.Dispose();
    }

    public void AddFruitHit(GameObject fruit)
    {
        fruitsHit.Add(fruit);
    }

    public void ClearFruitsHit()
    {
        fruitsHit.Clear();
    }
}
