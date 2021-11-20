using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using UnityEngine;

public class Madgwick {
  // dll api
  public class Impl {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionVector3 {
      [MarshalAs(UnmanagedType.R4)]
      public float x;
      [MarshalAs(UnmanagedType.R4)]
      public float y;
      [MarshalAs(UnmanagedType.R4)]
      public float z;

      public FusionVector3(float x_, float y_, float z_) {
        x = x_;
        y = y_;
        z = z_;
      }

      public FusionVector3(float a) {
        x = a;
        y = a;
        z = a;
      }

      public FusionVector3(Vector3 vec) {
        x = vec.x;
        y = vec.y;
        z = vec.z;
      }

      public static FusionVector3 operator *(FusionVector3 a, FusionVector3 b) {
        return new FusionVector3(a.x * b.x, a.y * b.y, a.z * b.z);
      }

      public static FusionVector3 operator /(FusionVector3 a, FusionVector3 b) {
        if (b.x == 0 || b.y == 0 || b.z == 0) {
          throw new DivideByZeroException();
        }
        return new FusionVector3(a.x / b.x, a.y / b.y, a.z / b.z);
      }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionQuaternion {
      [MarshalAs(UnmanagedType.R4)]
      public float w;
      [MarshalAs(UnmanagedType.R4)]
      public float x;
      [MarshalAs(UnmanagedType.R4)]
      public float y;
      [MarshalAs(UnmanagedType.R4)]
      public float z;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionEulerAngles {
      [MarshalAs(UnmanagedType.R4)]
      public float roll;
      [MarshalAs(UnmanagedType.R4)]
      public float pitch;
      [MarshalAs(UnmanagedType.R4)]
      public float yaw;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionRotationMatrix {
      [MarshalAs(UnmanagedType.R4)]
      public float xx;
      [MarshalAs(UnmanagedType.R4)]
      public float xy;
      [MarshalAs(UnmanagedType.R4)]
      public float xz;
      [MarshalAs(UnmanagedType.R4)]
      public float yx;
      [MarshalAs(UnmanagedType.R4)]
      public float yy;
      [MarshalAs(UnmanagedType.R4)]
      public float yz;
      [MarshalAs(UnmanagedType.R4)]
      public float zx;
      [MarshalAs(UnmanagedType.R4)]
      public float zy;
      [MarshalAs(UnmanagedType.R4)]
      public float zz;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionBias {
      [MarshalAs(UnmanagedType.R4)]
      public float threshold;
      [MarshalAs(UnmanagedType.R4)]
      public float samplePeriod;
      [MarshalAs(UnmanagedType.R4)]
      public float filterCoefficient;
      [MarshalAs(UnmanagedType.R4)]
      public float stationaryTimer;
      [MarshalAs(UnmanagedType.Struct)]
      public FusionVector3 gyroscopeBias;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionAhrs {
      [MarshalAs(UnmanagedType.R4)]
      public float gain;
      [MarshalAs(UnmanagedType.R4)]
      public float minimumMagneticFieldSquared;
      [MarshalAs(UnmanagedType.R4)]
      public float maximumMagneticFieldSquared;
      [MarshalAs(UnmanagedType.Struct)]
      public FusionQuaternion quaternion; // Earth relative to the sensor
      [MarshalAs(UnmanagedType.Struct)]
      public FusionVector3 linearAcceleration;
      [MarshalAs(UnmanagedType.R4)]
      public float rampedGain;
      [MarshalAs(UnmanagedType.Bool)]
      public float zeroYawPadding;
    }

    [DllImport("MadgwickDll.dll", EntryPoint = "FUSION_ROTATION_MATRIX_IDENTITY", CharSet = CharSet.Unicode)]
    public static extern FusionRotationMatrix FUSION_ROTATION_MATRIX_IDENTITY();

    [DllImport("MadgwickDll.dll", EntryPoint = "FUSION_VECTOR3_ZERO", CharSet = CharSet.Unicode)]
    public static extern FusionVector3 FUSION_VECTOR3_ZERO();

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionAhrsInitialise", CharSet = CharSet.Unicode)]
    public static extern void FusionAhrsInitialise(ref FusionAhrs fusionAhrs,
                                                   float gain);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionAhrsSetMagneticField", CharSet = CharSet.Unicode)]
    public static extern void FusionAhrsSetMagneticField(ref FusionAhrs fusionAhrs,
                                                         float minimumMagneticField,
                                                         float maximumMagneticField);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionAhrsUpdate", CharSet = CharSet.Unicode)]
    public static extern void FusionAhrsUpdate(ref FusionAhrs fusionAhrs,
                                               FusionVector3 gyroscope,
                                               FusionVector3 accelerometer,
                                               FusionVector3 magnetometer,
                                               float samplePeriod);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionAhrsGetQuaternion", CharSet = CharSet.Unicode)]
    public static extern FusionQuaternion FusionAhrsGetQuaternion(ref FusionAhrs fusionAhrs);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionQuaternionToEulerAngles", CharSet = CharSet.Unicode)]
    public static extern FusionEulerAngles FusionQuaternionToEulerAngles(
                                              FusionQuaternion quaternion);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionBiasInitialise", CharSet = CharSet.Unicode)]
    public static extern void FusionBiasInitialise(ref FusionBias fusionBias,
                                                   float threshold,
                                                   float samplePeriod);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionBiasUpdate", CharSet = CharSet.Unicode)]
    public static extern FusionVector3 FusionBiasUpdate(ref FusionBias fusionBias,
                                               FusionVector3 gyroscope);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionBiasIsActive", CharSet = CharSet.Unicode)]
    public static extern bool FusionBiasIsActive(ref FusionBias fusionBias);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionCalibrationInertial", CharSet = CharSet.Unicode)]
    public static extern FusionVector3 FusionCalibrationInertial(
        FusionVector3 uncalibrated, FusionRotationMatrix misalignment,
        FusionVector3 sensitivity, FusionVector3 bias);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionCalibrationMagnetic", CharSet = CharSet.Unicode)]
    public static extern FusionVector3 FusionCalibrationMagnetic(
        FusionVector3 uncalibrated, FusionRotationMatrix softIronMatrix,
        FusionVector3 hardIronBias);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionCompassCalculateHeading", CharSet = CharSet.Unicode)]
    public static extern float FusionCompassCalculateHeading(
        FusionVector3 accelerometer, FusionVector3 magnetometer);
  }

  private enum Sensor {
    GYRO, ACCEL, MAG
  }

  private const float  _Lsb = 65535;
  private readonly Impl.FusionVector3 _LsbVec = new Impl.FusionVector3(_Lsb);
  private readonly Impl.FusionVector3 _Vec1k = new Impl.FusionVector3(1000);

  // TODO: Get this value experimentally as right now this was a random guess
  private readonly Impl.FusionVector3
    _HardIronBias = new Impl.FusionVector3(100, 100, 0); // uT

  private const float _DesiredSamplePeriod = 0.01F; // sec
  private const float _StationaryThreshold = 8f; // dps
  private const float _Gain = 5f;
  private const float _MinMagField = 20f; // uT
  private const float _MaxMagField = 70f; // uT
  private Impl.FusionBias _bias = new Impl.FusionBias();
  private Impl.FusionAhrs _ahrs = new Impl.FusionAhrs();

  public Madgwick() {
    // Initialise gyroscope bias correction algorithm
    Impl.FusionBiasInitialise(ref _bias, _StationaryThreshold, _DesiredSamplePeriod);
    // Initialise AHRS algorithm
    Impl.FusionAhrsInitialise(ref _ahrs, _Gain);
    // Set optional magnetic field limits
    Impl.FusionAhrsSetMagneticField(ref _ahrs, _MinMagField, _MaxMagField);
  }

  public void AhrsUpdate(Vector3 rawGyro_   /*dps*/,
                         Vector3 rawAccel_  /*mg*/,
                         Vector3 rawMag_    /*uT*/,
                         float samplePeriod /*seconds*/) {
    // Convert to struct type expected by DLL
    Impl.FusionVector3 rawGyro = new Impl.FusionVector3(rawGyro_);
    Impl.FusionVector3 rawAccel = new Impl.FusionVector3(rawAccel_);
    Impl.FusionVector3 rawMag = new Impl.FusionVector3(rawMag_);

    // Get calibrated value for each sensor
    Logger.Debug($"--------------- AhrsUpdate ---------------");
    Logger.Debug($"raw gyro: x={rawGyro.x}, y={rawGyro.y}, z={rawGyro.z}");
    Logger.Debug($"raw accel: x={rawAccel.x}, y={rawAccel.y}, z={rawAccel.z}");
    Logger.Debug($"raw mag: x={rawMag.x}, y={rawMag.y}, z={rawMag.z}");

    _bias.samplePeriod = samplePeriod;
    Impl.FusionVector3 calibGyro = Impl.FusionBiasUpdate(ref _bias, rawGyro);
    Impl.FusionVector3 calibAccel = rawAccel / _Vec1k;
    Impl.FusionVector3 calibMag = Impl.FusionCalibrationMagnetic(
        rawMag, Impl.FUSION_ROTATION_MATRIX_IDENTITY(), _HardIronBias);

    Logger.Debug($"Bias correction is active={Impl.FusionBiasIsActive(ref _bias)}");
    Logger.Debug($"calib gyro: x={calibGyro.x}, y={calibGyro.y}, z={calibGyro.z}");
    Logger.Debug($"calib accel: x={calibAccel.x}, y={calibAccel.y}, z={calibAccel.z}");
    Logger.Debug($"calib mag: x={calibMag.x}, y={calibMag.y}, z={calibMag.z}");
    Logger.Debug($"------------------------------------- ");

    // Update AHRS algorithm
    Impl.FusionAhrsUpdate(ref _ahrs, calibGyro, calibAccel, calibMag, samplePeriod);
  }

  public Quaternion GetQuaternion() {
    Impl.FusionQuaternion q = Impl.FusionAhrsGetQuaternion(ref _ahrs);
    // Flip z and y axis to work properly with Unity coordinate frame.
    return new Quaternion(q.x, q.z, q.y, q.w);
  }

  public Vector3 GetEulerAngles() {
    return GetQuaternion().eulerAngles;
  }
}

