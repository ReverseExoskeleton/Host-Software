﻿using System;
using UnityEngine;


public readonly struct SensorSample {
  public readonly struct ImuSample {
    private const float _MagScale = 1 / 0.15f; // constant from getMagUT
    private const float _AccelScale = 16.384f; // default setting gpm2 (ICM_20948_ACCEL_CONFIG_FS_SEL_e)
    private const float _GyroScale = 16.384f; // default setting dps250 (ICM_20948_GYRO_CONFIG_1_FS_SEL_e)
    private const int _NumBytes = ImuSampleNumBytes;

    public Vector3 LinAccel { get; }
    public Vector3 AngVel { get; }
    public Vector3 MagField { get; }

    public ImuSample(byte[] dataBuffer) {
      float[] dataArray = ConvertBufferToDataArr(dataBuffer);
      LinAccel = new Vector3(x: dataArray[0],
                             y: dataArray[1],
                             z: dataArray[2]);
      AngVel = new Vector3(x: dataArray[3],
                           y: dataArray[4],
                           z: dataArray[5]);
      MagField = new Vector3(x: dataArray[6],
                             y: dataArray[7],
                             z: dataArray[8]);
    }

    private static float[] ConvertBufferToDataArr(byte[] dataBuffer) {
      float[] dataArray = new float[dataBuffer.Length / 2];

      for (int i = 0; i < dataArray.Length; i += 2) {
        float curFloat = GetFloatFromTwoBytes(dataBuffer, offset: i);

        if (i < _NumBytes / 3) {
          dataArray[i / 2] = curFloat / _AccelScale;
        } else if (_NumBytes / 3 <= i && i < 2 * _NumBytes / 3) {
          dataArray[i / 2] = curFloat / _GyroScale;
        } else if (2 * _NumBytes / 3 <= i && i < _NumBytes) {
          dataArray[i / 2] = curFloat / _MagScale;
        } else {
          throw new Exception("Packet range calculations were wrong. Oops.");
        }
      }
      return dataArray;
    }
  }

  // IMU Data + Elbow Angle
  // IMU:
  //   Nine DOF, each with 16-bit precision.
  public const int ImuSampleNumBytes = 2 * 9; 
  // Elbow Angle:
  //   One 16-bit float.
  public const int ElbowAngNumBytes = 2; 
  public const int NumBytes = ImuSampleNumBytes + ElbowAngNumBytes;

  public ImuSample Imu { get; }
  public float ElbowAngleDeg { get; }

  public SensorSample(byte[] dataBuffer) {
    Debug.Assert(dataBuffer.Length == NumBytes);

    byte[] imuBytes = new byte[ImuSampleNumBytes];
    byte[] elbowAngBytes = new byte[ElbowAngNumBytes];
    System.Buffer.BlockCopy(dataBuffer, 0, imuBytes, 0, ImuSampleNumBytes);
    System.Buffer.BlockCopy(dataBuffer, ImuSampleNumBytes, elbowAngBytes,
                            0, ElbowAngNumBytes);

    Imu = new ImuSample(imuBytes);
    ElbowAngleDeg = GetElbowAngleFromBuffer(elbowAngBytes);
  }

  private static float GetElbowAngleFromBuffer(byte[] dataBuffer) {
    return GetFloatFromTwoBytes(dataBuffer, offset: 0);
  }

  private static float GetFloatFromTwoBytes(byte[] dataBuffer, int offset) {
    Debug.Assert(offset > 0 && offset <= dataBuffer.Length - 2);
    // ----------------- TODO: Remove this later after debugging --------------
    byte[] float16Bytes = { dataBuffer[offset], dataBuffer[offset + 1] };
    Logger.Debug($"Original bytes = " +
                 $"{BitConverter.ToString(float16Bytes).Replace("-", "")}");
    // ------------------------------------------------------------------------
    short rawInt = BitConverter.ToInt16(dataBuffer, offset);
    // ----------------- TODO: Remove this later after debugging --------------
    Logger.Debug($"Int recovered bytes = { BitConverter.GetBytes(rawInt) }");
    // ------------------------------------------------------------------------
    return rawInt;
  }
}

