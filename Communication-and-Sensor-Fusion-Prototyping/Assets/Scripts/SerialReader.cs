using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public readonly struct Measurement3D {
  public float X { get; }
  public float Y { get; }
  public float Z { get; }
  public Measurement3D(float x, float y, float z) {
    X = x;
    Y = y;
    Z = z;
  }
}

public readonly struct ImuSample {
  public Measurement3D LinAccel { get; }
  public Measurement3D AngVel { get; }
  public Measurement3D MagField { get; }

  public ImuSample(byte[] dataBuffer) {
    float[] dataArray = ConvertBufferToDataArr(dataBuffer);
    // -------------------- Debugging --------------------
    Debug.Log("Received Floats = " + dataArray.ToString());
    // ---------------------------------------------------

    LinAccel = new Measurement3D(/*X=*/dataArray[0],
                                 /*Y=*/dataArray[1],
                                 /*Z=*/dataArray[2]);
    AngVel = new Measurement3D(/*X=*/dataArray[3],
                               /*Y=*/dataArray[4],
                               /*Z=*/dataArray[5]);
    MagField = new Measurement3D(/*X=*/dataArray[6],
                                 /*Y=*/dataArray[7],
                                 /*Z=*/dataArray[8]);
  }

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

public class HardwareConfigurationException : Exception {
  public HardwareConfigurationException() { }
  public HardwareConfigurationException(string message) : base(message) { }
  public HardwareConfigurationException(string message, Exception inner) :
    base(message, inner) { }
}

public class SerialReader {
  private const int _PacketNumBytes = 2 * 9; // 9 DOF each with 16-bit precision
  private const int _BufferQueueCapacity = 10; // Arbitrary
  private const int _MaxNumYieldsToReaderThread = 5;

  private SerialPort _serialPort;
  private Queue<byte[]> _bufferQueue = new Queue<byte[]>(_BufferQueueCapacity);

  public SerialReader(string portName = "COM1", int baudRate = 19200,
                      Parity parity = Parity.None, int dataBits = 8,
                      StopBits stopBits = StopBits.One) {
      _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits) {
        Handshake = Handshake.None
      };
    _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

    try {
      _serialPort.Open();
    } catch (IOException e) {
      throw new HardwareConfigurationException("Failed to configure serial " +
                                                "interface: ", e);
    }
  }

  public ImuSample[] GetImuSamples() {
    ImuSample[] samples = { };
    Assert.AreEqual(0, samples.Length);

    for (int i = 0; i < _MaxNumYieldsToReaderThread; i++) {
      lock (_bufferQueue) {
        if (_bufferQueue.Count <= 0) { break; }
        samples = new ImuSample[_bufferQueue.Count];
      }
      if (samples.Length > 0) { return samples; }
    }
    throw new Exception("Did not receive serial packets after yielding main thread" +
      _MaxNumYieldsToReaderThread + " times.");
  }

  private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
    lock (_bufferQueue) {
      if (_bufferQueue.Count >= _BufferQueueCapacity) {
        Debug.LogError("Buffer queue with capacity " + _BufferQueueCapacity +
          " reached limit. Dequeing 1st element in queue.");
        _ = _bufferQueue.Dequeue();
      }
      byte[] inBuffer = new byte[_PacketNumBytes];
      int numBytesRead = _serialPort.Read(inBuffer, /*offset=*/0, _PacketNumBytes);
      Assert.AreEqual(_PacketNumBytes, numBytesRead);
      // -------------------- Debugging --------------------
      Debug.Log("Received Bytes = " + inBuffer.ToString());
      // ---------------------------------------------------
      _bufferQueue.Enqueue(inBuffer);
    }
  }
}

