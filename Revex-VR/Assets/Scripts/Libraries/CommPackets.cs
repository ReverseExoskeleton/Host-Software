﻿using System;
using UnityEngine;


public readonly struct SensorSample {
  // IMU Data + Elbow Angle
  // IMU:
  //   Nine DOF, each with 16-bit precision.
  public ImuSample Imu { get; }
  public const int ImuSampleNumBytes = 2 * 9; 
  // Elbow Angle:
  //   One 16-bit float.
  public float ElbowAngleDeg { get; }
  public const int ElbowAngNumBytes = 2;
  public const int NumBytes = ImuSampleNumBytes + ElbowAngNumBytes;

  // --------------- Elbow Angle Calculation --------------- 
  private const float _ElbowMinDeg = 25;
  private const float _ElbowMinAdc = 1194;
  private const float _ElbowMaxDeg = 180;
  private const float _ElbowMaxAdc = 2594;
  private const float _ElbowEqnSlope = (_ElbowMaxDeg - _ElbowMinDeg) /
                                       (_ElbowMaxAdc - _ElbowMinAdc);
  private const float _ElbowEqnIntcpt = _ElbowMaxDeg - 
                                        (_ElbowEqnSlope * _ElbowMaxAdc);
  // -------------------------------------------------------

  // --------------- Scaling constants --------------- 
  // constant from getMagUT
  // Arduino Library: https://github.com/sparkfun/SparkFun_ICM-20948_ArduinoLibrary/blob/74d9c1e4103d2ca11d1645489108a152095d15e7/src/ICM_20948.cpp#L163
  public const float MagScale = 1 / 0.15f; // LSB / uT
  // Arduino Library default setting gpm2 (ICM_20948_ACCEL_CONFIG_FS_SEL_e): https://github.com/sparkfun/SparkFun_ICM-20948_ArduinoLibrary/blob/74d9c1e4103d2ca11d1645489108a152095d15e7/src/ICM_20948.cpp#L887
  public const float AccelScale = 16.384f; // LSB / mg   <-- gravity g's not grams
  public const float AccelScaleG = AccelScale * 1000; // LSB / g   <-- gravity g's not grams
  // Arduino Library default setting dps250 (ICM_20948_GYRO_CONFIG_1_FS_SEL_e): https://github.com/sparkfun/SparkFun_ICM-20948_ArduinoLibrary/blob/74d9c1e4103d2ca11d1645489108a152095d15e7/src/ICM_20948.cpp#L888
  public const float GyroScale = 131f; // LSB / dps
  // -------------------------------------------------

  public SensorSample(byte[] dataBuffer) {
    byte[] elbowAngBytes = new byte[ElbowAngNumBytes];
    byte[] imuBytes = new byte[ImuSampleNumBytes];
    System.Buffer.BlockCopy(dataBuffer, 0, elbowAngBytes, 0, ElbowAngNumBytes);
    System.Buffer.BlockCopy(dataBuffer, ElbowAngNumBytes, imuBytes,
                            0, ImuSampleNumBytes);

    Imu = new ImuSample(imuBytes);
    ElbowAngleDeg = GetElbowAngleFromBuffer(elbowAngBytes);
  }

  private static float GetElbowAngleFromBuffer(byte[] dataBuffer) {
    float elbowAdc = GetFloatFromTwoBytes(dataBuffer, offset: 0);
    Logger.Debug($"Elbow ADC value = {elbowAdc}");
    return (elbowAdc * _ElbowEqnSlope) + _ElbowEqnIntcpt;
  }

  private static float GetFloatFromTwoBytes(byte[] dataBuffer, int offset) {
    Debug.Assert(offset >= 0 && offset <= dataBuffer.Length - 2);

    // Use Big Endian
    byte msb = dataBuffer[offset + 1];
    dataBuffer[offset + 1] = dataBuffer[offset];
    dataBuffer[offset] = msb;

    short rawInt = BitConverter.ToInt16(dataBuffer, offset);
    return rawInt;
  }

  public readonly struct ImuSample {
    private const int _NumBytes = ImuSampleNumBytes;

    public Vector3 LinAccel { get; }
    public Vector3 AngVel { get; }
    public Vector3 MagField { get; }

    public ImuSample(byte[] dataBuffer) {
      float[] dataArray = ConvertBufferToDataArr(dataBuffer);
      AngVel = new Vector3(x: dataArray[0],
                             y: dataArray[1],
                             z: dataArray[2]);
      LinAccel = new Vector3(x: dataArray[3],
                           y: dataArray[4],
                           z: dataArray[5]);
      MagField = new Vector3(x: dataArray[6],
                             y: dataArray[7],
                             z: dataArray[8]);
    }

    private static float[] ConvertBufferToDataArr(byte[] dataBuffer) {
      float[] dataArray = new float[dataBuffer.Length / 2];

      for (int i = 0; i < dataBuffer.Length; i += 2) {
        float curFloat = GetFloatFromTwoBytes(dataBuffer, offset: i);

        if (i < _NumBytes / 3) {
          Logger.Debug($"gyro int {i} = {curFloat}");
          dataArray[i / 2] = curFloat / GyroScale;
        } else if (_NumBytes / 3 <= i && i < 2 * _NumBytes / 3) {
          Logger.Debug($"accel int {i} = {curFloat}");
          dataArray[i / 2] = curFloat / AccelScale;
        } else if (2 * _NumBytes / 3 <= i && i < _NumBytes) {
          Logger.Debug($"mag int {i} = {curFloat}");
          dataArray[i / 2] = curFloat / MagScale;
        } else {
          throw new Exception("Packet range calculations were wrong. Oops.");
        }
      }
      return dataArray;
    }
  }
}

public readonly struct HapticFeedback {
  public int Frequency { get; }
  public int DutyCycle { get; }
  public byte Payload { get; }

  private const int _DutyCycleBits = 5;
  private const int _FrequencyBits = 3;
  private const int _DutyCycleRes = 1 << _DutyCycleBits;
  private const int _FrequencyRes = 1 << _FrequencyBits;

  public HapticFeedback(float dutyCyclePercent, float frequencyPercent) {
    int dutyCycle = (int)Math.Round(dutyCyclePercent * _DutyCycleRes);
    int frequency = (int)Math.Round(frequencyPercent * _FrequencyRes);

    Payload = (byte)((dutyCycle << _FrequencyBits) + frequency);

    Frequency = (int)(frequency * (15f / 8) + 10);
    DutyCycle = (int)(dutyCycle * (100f / 32));
  }
}

