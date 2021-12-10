using System;
using System.Collections.Generic;
using UnityEngine;

public class NinjaArmController : MonoBehaviour
{
    public Transform imu;
    public Transform shoulder;
    public Transform elbow;

    // -------------------- Game --------------------
    public float batteryVoltage = 0;
    public List<GameObject> fruitsHit = new List<GameObject>();

    private Vector3 shoulderStart;

    public PsscDeviceStatus _status = PsscDeviceStatus.Asleep;
    public Tranceiver tranceiver;
    public bool useBleTranceiver = true;
    private float _timeSinceLastPacketS = 0; // sec

    // --------------- Arm Estimation ---------------
    public Madgwick fusion;

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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Logger.Warning("Correcting for bias");
            Quaternion bias_deg = Quaternion.Euler(0, 90, 0);
            fusion = new Madgwick(Quaternion.identity);
        }

        PsscDeviceStatus initialStatus = _status;
        switch (_status)
        {
            case PsscDeviceStatus.Asleep:
                if (tranceiver.DeviceIsAwake(forceDeviceSearch: true))
                    _status = PsscDeviceStatus.Connecting;
                break;
            case PsscDeviceStatus.Connecting:
                if (tranceiver.TryEstablishConnection())
                    _status = PsscDeviceStatus.ArmEstimation;
                break;
            case PsscDeviceStatus.ArmEstimation:
                if (!tranceiver.DeviceIsAwake(forceDeviceSearch: false))
                {
                    _status = PsscDeviceStatus.Asleep;
                    _timeSinceLastPacketS = 0;
                    break;
                }

                if (!UpdateSensorData()) return;
                UpdateTransforms();

                break;
            default:
                throw new Exception($"Unknown case {_status}.");
        }

        if (initialStatus != _status)
        {
            Logger.Debug($"Status changed from {initialStatus} to {_status}.");
        }
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

            UpdateElbowAngle(sample.ElbowAngleDeg);

            batteryVoltage = sample.BatteryVoltage; // Do something with this ...
        }

        return true;
    }

    private void UpdateTransforms()
    {
        UpdateRotation(fusion.GetQuaternion());
    }

    private void OnApplicationQuit()
    {
        tranceiver?.Dispose();
    }

    public void UpdateRotation(Quaternion imuRot)
    {
        imu.rotation = imuRot;
        imu.position += shoulderStart - shoulder.position;  // Make it rotate around the shoulder
    }

    public void UpdateElbowAngle(float angle)
    {
        elbow.localRotation = Quaternion.Euler(0f, 0f, 180f - angle);
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
