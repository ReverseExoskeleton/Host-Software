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
    public float batteryVoltage = 0;
    public List<GameObject> fruitsHit = new List<GameObject>();


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
        fusion = new Madgwick(Quaternion.identity);
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

    public void Recalibrate() {
        Logger.Debug("Recalibrating");
        Quaternion bias_deg = Quaternion.Euler(0, 90, 0);
        fusion = new Madgwick(Quaternion.identity);
    }

  private bool UpdateSensorData()
    {
        _timeSinceLastPacketS += Time.deltaTime;

        List<SensorSample> samples = new List<SensorSample>();
        if (tranceiver?.TryGetSensorData(out samples) == false) return false;
        float samplePeriod = _timeSinceLastPacketS / samples.Count;
        //fusion.SamplePeriod = samplePeriod;
        _timeSinceLastPacketS = 0;

        foreach (SensorSample sample in samples)
        {
            fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                              sample.Imu.MagField, samplePeriod);
            elbowEma.Update(sample.ElbowAngleDeg);

            batteryVoltage = sample.BatteryVoltage; // Do something with this ...
        }

        return true;
    }

    private void UpdateTransforms()
    {
        imu.rotation = fusion.GetQuaternion();
        imu.position += shoulderStart - shoulder.position;  // Make it rotate around the shoulder

        elbow.localRotation = Quaternion.Euler(0f, 0f, 180f - elbowEma.Current());
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
