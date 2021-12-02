using System;

using Vector3D = Madgwick.Impl.FusionVector3;

public readonly struct ImuSample {
  private const int _PacketNumBytes = 2 * 9; // 9 DOF each with 16-bit precision
  public Vector3D LinAccel { get; }
  public Vector3D AngVel { get; }
  public Vector3D MagField { get; }

  public ImuSample(byte[] dataBuffer) {
    float[] dataArray = ConvertBufferToDataArr(dataBuffer);
    LinAccel = new Vector3D(/*X=*/dataArray[0],
                            /*Y=*/dataArray[1],
                            /*Z=*/dataArray[2]);
    AngVel = new Vector3D(/*X=*/dataArray[3],
                          /*Y=*/dataArray[4],
                          /*Z=*/dataArray[5]);
    MagField = new Vector3D(/*X=*/dataArray[6],
                            /*Y=*/dataArray[7],
                            /*Z=*/dataArray[8]);
  }

  // Adapted from Mohsen Sarkars answer at https://stackoverflow.com/a/37761168 (CC BY-SA 3.0)
  private static float GetTwoByteFloat(byte HO, byte LO) {
    int intVal = BitConverter.ToInt32(new byte[] { HO, LO, 0, 0 }, 0);

    int mant = intVal & 0x03ff;
    int exp = intVal & 0x7c00;
    if (exp == 0x7c00) {
      exp = 0x3fc00;
    } else if (exp != 0) {
      exp += 0x1c000;
      if (mant == 0 && exp > 0x1c400)
        return BitConverter.ToSingle(BitConverter.GetBytes(
          ((intVal & 0x8000) << 16) | (exp << 13) | 0x3ff), 0);
    } else if (mant != 0) {
      exp = 0x1c400;
      do {
        mant <<= 1;
        exp -= 0x400;
      } while ((mant & 0x400) == 0);
      mant &= 0x3ff;
    }
    return BitConverter.ToSingle(BitConverter.GetBytes(
      ((intVal & 0x8000) << 16) | ((exp | mant) << 13)), 0);
  }

  private static float[] ConvertBufferToDataArr(byte[] dataBuffer) {
    float[] dataArray = new float[dataBuffer.Length / 2];
    for (int i = 0; i < dataBuffer.Length; i += 2) {
      dataArray[i / 2] = GetTwoByteFloat(dataBuffer[i], dataBuffer[i + 1]);
    }
    return dataArray;
  }
}

public readonly struct RawImuSample {
  private const int _PacketNumBytes = 2 * 9; // 9 DOF each with 16-bit precision
  private const float _MagScale = 1/0.15f; // constant from getMagUT
  private const float _AccelScale =  16.384f; // default setting gpm2 (ICM_20948_ACCEL_CONFIG_FS_SEL_e)
  private const float _GyroScale =  16.384f; // default setting dps250 (ICM_20948_GYRO_CONFIG_1_FS_SEL_e)

  public Vector3D LinAccel { get; }
  public Vector3D AngVel { get; }
  public Vector3D MagField { get; }

  public RawImuSample(byte[] dataBuffer) {
    float[] dataArray = ConvertBufferToDataArr(dataBuffer);
    LinAccel = new Vector3D(/*X=*/dataArray[0],
                            /*Y=*/dataArray[1],
                            /*Z=*/dataArray[2]);
    AngVel = new Vector3D(/*X=*/dataArray[3],
                          /*Y=*/dataArray[4],
                          /*Z=*/dataArray[5]);
    MagField = new Vector3D(/*X=*/dataArray[6],
                            /*Y=*/dataArray[7],
                            /*Z=*/dataArray[8]);
  }

  private static float[] ConvertBufferToDataArr(byte[] dataBuffer) {
    float[] dataArray = new float[dataBuffer.Length / 2];

    for (int i = 0; i < dataArray.Length; i += 2) {
      byte[] origBytes = { dataBuffer[i], dataBuffer[i + 1] };
      Logger.Debug($"Original bytes = {origBytes}");
      short rawInt = BitConverter.ToInt16(dataBuffer, i);
      Logger.Debug($"Int recovered bytes = { BitConverter.GetBytes(rawInt) }");

      if (i < _PacketNumBytes / 3) {
        dataArray[i / 2] = ((float)rawInt) / _AccelScale;
      } else if (_PacketNumBytes / 3 <= i && i < 2 * _PacketNumBytes / 3) {
        dataArray[i / 2] = ((float)rawInt) / _GyroScale;
      } else if (2 * _PacketNumBytes / 3 <= i && i < _PacketNumBytes) {
        dataArray[i / 2] = ((float)rawInt) / _MagScale;
      } else {
        throw new Exception("Packet range calculations were wrong. Oops.");
      }

    }

    return dataArray;
  }
}

