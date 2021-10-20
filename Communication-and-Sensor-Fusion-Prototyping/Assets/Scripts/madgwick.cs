using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;

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

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionCalibrationInertial", CharSet = CharSet.Unicode)]
    public static extern FusionVector3 FusionCalibrationInertial(
        FusionVector3 uncalibrated, FusionRotationMatrix misalignment,
        FusionVector3 sensitivity, FusionVector3 bias);

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionCalibrationMagnetic", CharSet = CharSet.Unicode)]
    public static extern FusionVector3 FusionCalibrationMagnetic(
        FusionVector3 uncalibrated, FusionRotationMatrix softIronMatrix,
        FusionVector3 hardIronBias);
  }

  private enum Sensor {
    GYRO, ACCEL, MAG
  }

  // Dictionaries that map from sensor full-scale range to sensitivity
  private const float _LSB = 65535; // sec
  private readonly IDictionary<int, float> _GyroSensitivities = new Dictionary<int, float>(){
    {250, 1/131F}, // dps, (dps/LSB)
    {500, 1/65.5F},
    {1000, 1/32.8F},
    {2000, 1/16.4F}};
  private readonly IDictionary<int, float> _AccelSensitivities = new Dictionary<int, float>(){
    {2, 1/16384F}, // G, (g/LSB)
    {4, 1/8192F},
    {8, 1/4096F},
    {16, 1/2048F}};
  private readonly float _MagSensitivity = 0.15F; // (uT/LSB)
  //private readonly IDictionary<int, float> _GyroSensitivities = new Dictionary<int, float>(){
  //  {250, 131F},
  //  {500, 65.5F},
  //  {1000, 32.8F},
  //  {2000, 16.4F}};
  //private readonly IDictionary<int, float> _AccelSensitivities = new Dictionary<int, float>(){
  //  {2, 16384F},
  //  {4, 8192F},
  //  {8, 4096F},
  //  {16, 2048F}};
  //private readonly float _MagSensitivity = 0.15F * _LSB; // uT
  private const float _DesiredSamplePeriod = 0.01F; // sec
  private const int _GyroFSRange = 500; // dps
  private const int _AccelFSRange = 4; // G
  private const float _StationaryThreshold = 0.5f; // dps
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

  private Impl.FusionVector3 ScaleVecByLSB(Impl.FusionVector3 v, bool isAccel = false) {
    v.x /= _LSB;
    v.y /= _LSB;
    v.z /= _LSB;
    if (isAccel) {
      v.x /= 1000;
      v.y /= 1000;
      v.z /= 1000;
    }
    return v;
  }

  private Impl.FusionVector3 GetSensitivity(Sensor sensor) {
    float sensitivity;
    switch (sensor) {
      case Sensor.GYRO: {
          sensitivity = _GyroSensitivities[_GyroFSRange];
          break;
      }
      case Sensor.ACCEL: {
          sensitivity = _AccelSensitivities[_AccelFSRange];
          break;
      }
      case Sensor.MAG: {
          sensitivity = _MagSensitivity;
          break;
      }
      default: throw new InvalidEnumArgumentException();
    }
    return new Impl.FusionVector3(sensitivity, sensitivity, sensitivity);
  }

  public void Update(Impl.FusionVector3 rawGyro  /*dps*/,
                     Impl.FusionVector3 rawAccel /*mg*/,
                     Impl.FusionVector3 rawMag   /*uT*/,
                     float samplePeriod          /*seconds*/) {
    // Get calibrated value for each sensor
    Impl.FusionVector3 calibGyro = Impl.FusionCalibrationInertial(
        ScaleVecByLSB(rawGyro), Impl.FUSION_ROTATION_MATRIX_IDENTITY(),
        GetSensitivity(Sensor.GYRO), Impl.FUSION_VECTOR3_ZERO());
    Impl.FusionVector3 calibAccel = Impl.FusionCalibrationInertial(
        ScaleVecByLSB(rawAccel, true), Impl.FUSION_ROTATION_MATRIX_IDENTITY(),
        GetSensitivity(Sensor.ACCEL), Impl.FUSION_VECTOR3_ZERO());
    Impl.FusionVector3 calibMag = Impl.FusionCalibrationMagnetic(
        ScaleVecByLSB(rawMag), Impl.FUSION_ROTATION_MATRIX_IDENTITY(),
        GetSensitivity(Sensor.MAG));

    // Update gyroscope bias correction algorithm
    _bias.samplePeriod = samplePeriod;
    calibGyro = Impl.FusionBiasUpdate(ref _bias, calibGyro);

    // Update AHRS algorithm
    Impl.FusionAhrsUpdate(ref _ahrs, calibGyro, calibAccel, calibMag, samplePeriod);
  }

  public Impl.FusionQuaternion GetQuaternion() {
    return Impl.FusionAhrsGetQuaternion(ref _ahrs);
  }

  public Impl.FusionEulerAngles GetEulerAngles() {
    return Impl.FusionQuaternionToEulerAngles(GetQuaternion());
  }
}
