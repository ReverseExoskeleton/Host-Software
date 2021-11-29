using System;
using UnityEngine;


public readonly struct SensorSample {
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
          dataArray[i / 2] = curFloat / GyroScale;
        } else if (_NumBytes / 3 <= i && i < 2 * _NumBytes / 3) {
          dataArray[i / 2] = curFloat / AccelScale;
        } else if (2 * _NumBytes / 3 <= i && i < _NumBytes) {
          dataArray[i / 2] = curFloat / MagScale;
        } else {
          throw new Exception("Packet range calculations were wrong. Oops.");
        }
      }
      Logger.Debug($"Data array recovered = { String.Join(", ", dataArray) }");
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

  // --------------- Scaling constants --------------- 
  // constant from getMagUT
  // Arduino Library: https://github.com/sparkfun/SparkFun_ICM-20948_ArduinoLibrary/blob/74d9c1e4103d2ca11d1645489108a152095d15e7/src/ICM_20948.cpp#L163
  public const float MagScale = 1 / 0.15f; // LSB / uT
  // Arduino Library default setting gpm2 (ICM_20948_ACCEL_CONFIG_FS_SEL_e): https://github.com/sparkfun/SparkFun_ICM-20948_ArduinoLibrary/blob/74d9c1e4103d2ca11d1645489108a152095d15e7/src/ICM_20948.cpp#L887
  public const float AccelScale = 16.384f; // LSB / mg   <-- gravity g's not grams
  public const float AccelScaleG = 16384f; // LSB / g   <-- gravity g's not grams
  // Arduino Library default setting dps250 (ICM_20948_GYRO_CONFIG_1_FS_SEL_e): https://github.com/sparkfun/SparkFun_ICM-20948_ArduinoLibrary/blob/74d9c1e4103d2ca11d1645489108a152095d15e7/src/ICM_20948.cpp#L888
  public const float GyroScale = 131f; // LSB / dps
  // -------------------------------------------------

  public ImuSample Imu { get; }
  public float ElbowAngleDeg { get; }

  public SensorSample(byte[] dataBuffer) {
    Debug.Assert(dataBuffer.Length == NumBytes);

    byte[] elbowAngBytes = new byte[ElbowAngNumBytes];
    byte[] imuBytes = new byte[ImuSampleNumBytes];
    System.Buffer.BlockCopy(dataBuffer, 0, elbowAngBytes, 0, ElbowAngNumBytes);
    System.Buffer.BlockCopy(dataBuffer, ElbowAngNumBytes, imuBytes,
                            0, ImuSampleNumBytes);

    Imu = new ImuSample(imuBytes);
    ElbowAngleDeg = GetElbowAngleFromBuffer(elbowAngBytes);
  }

  private static float GetElbowAngleFromBuffer(byte[] dataBuffer) {
    return GetFloatFromTwoBytes(dataBuffer, offset: 0);
  }

  private static float GetFloatFromTwoBytes(byte[] dataBuffer, int offset) {
    Debug.Assert(offset >= 0 && offset <= dataBuffer.Length - 2);
    short rawInt = BitConverter.ToInt16(dataBuffer, offset);
    return rawInt;
  }
}

