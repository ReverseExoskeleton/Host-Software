using System;
using System.Threading;
using System.Linq;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;

using Vector3D = Madgwick.Impl.FusionVector3;

public class HardwareConfigurationException : Exception {
  public HardwareConfigurationException() { }
  public HardwareConfigurationException(string message) : base(message) { }
  public HardwareConfigurationException(string message, Exception inner) :
    base(message, inner) { }
}

public class InvalidDataStartPacketException  : Exception {
  public InvalidDataStartPacketException () { }
  public InvalidDataStartPacketException (string message) : base(message) { }
  public InvalidDataStartPacketException (string message, Exception inner) :
    base(message, inner) { }
}

public class ReaderBufferFullException : Exception {
  public ReaderBufferFullException() { }
  public ReaderBufferFullException(string message) : base(message) { }
  public ReaderBufferFullException(string message, Exception inner) :
    base(message, inner) { }
}

public class PacketQueueEmptyException : Exception {
  public PacketQueueEmptyException() { }
  public PacketQueueEmptyException(string message) : base(message) { }
  public PacketQueueEmptyException(string message, Exception inner) :
    base(message, inner) { }
}

public class PacketQueueFullException : Exception {
  public PacketQueueFullException() { }
  public PacketQueueFullException(string message) : base(message) { }
  public PacketQueueFullException(string message, Exception inner) :
    base(message, inner) { }
}

public readonly struct ImuSample {
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

public class SerialReader {
  private const int _PacketNumBytes = 2 * 9; // 9 DOF each with 16-bit precision
  private const int _curInputBufferCapacity = 50; // Arbitrary
  private const int _PacketQueueCapacity = 100; // Arbitrary
  // TODO: May also want to send message back to IMU once received so then it
  // can start sending data packets
  private const string _DataStartString = "Data Start"; // Arbitrary
  private const int _MaxNumYieldsToReaderThread = 100;

  private SerialPort _serialPort;
  private byte[] _curInputBuffer = new byte[0];
  private Queue<byte[]> _packetQueue = new Queue<byte[]>(_PacketQueueCapacity);
  private bool _waitingForDataStart = true;
  private int _dataStartStrIdx = 0;
  private bool _isRunning = true;
  private bool _firstSerialDataEvent = true;

  public SerialReader(string portName = "COM3", int baudRate = 115200,
                      Parity parity = Parity.None, int dataBits = 8,
                      StopBits stopBits = StopBits.One) {
    _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits) {
      Handshake = Handshake.None,
    };

    try {
      _serialPort.Open();
    } catch (IOException e) {
      throw new HardwareConfigurationException($@"Failed to configure serial 
                                                 interface:{e}.");
    }

    // Idea from https://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
    byte[] buffer = new byte[_PacketNumBytes];
    void kickoffRead() {
      _ = _serialPort.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar) {
        try {
          int actualLength = _serialPort.BaseStream.EndRead(ar);
          byte[] received = new byte[actualLength];
          Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
          HandleSerialDataEvent(received);
        } catch (IOException e) {
          Logger.Debug(e); // TODO: Replace this with something to recover
        } catch (InvalidDataStartPacketException e) {
          Logger.Debug(e); // TODO: Replace this with something to recover
        } catch (ReaderBufferFullException e) {
          Logger.Debug(e); // TODO: Replace this with something to recover
        } catch (PacketQueueFullException e) {
          Logger.Debug(e); // TODO: Replace this with something to recover
        } catch (Exception e) {
          Logger.Error("Unknow Exception in kickoffRead: " + e);
        }

        if (_isRunning) {
          kickoffRead();
        } else {
          _serialPort.Close();
        }
      }, null);
    }
    kickoffRead();
  }

  public void WaitUntilReady() {
    while (_waitingForDataStart) Thread.Yield();
  }

  public void StopReading() {
    _isRunning = false;
  }

  public List<ImuSample> GetImuSamples() {
    for (int i = 0; i < _MaxNumYieldsToReaderThread; i++) {
      List<ImuSample> samples = null;
      lock (_packetQueue) {
        Logger.Debug($@"GetImuSamples got lock and there are 
            {_packetQueue.Count} entries in q");
        if (_packetQueue.Count <= 0) { break; }
        samples = new List<ImuSample>(_packetQueue.Count);
        while (_packetQueue.Count > 0) {
          samples.Add(new ImuSample(_packetQueue.Dequeue()));
        }
      }
      if (samples?.Any() ?? false) return samples;
      if (!Thread.Yield()) {
        Logger.Error("Failed to yield main thread.");
      }
    }
    throw new PacketQueueEmptyException($@"Did not receive serial data 
        packets after yielding main thread {_MaxNumYieldsToReaderThread} times.");
  }

  private void HandleSerialDataEvent(byte[] inputBuffer) {
    if (_firstSerialDataEvent) {
      Logger.Debug("Read tid =" + Thread.CurrentThread.ManagedThreadId);
      _firstSerialDataEvent = false;
    }

    if (_waitingForDataStart) {
      try {
        HandleDataStartString(inputBuffer);
      } catch (InvalidDataStartPacketException) {
        throw;
      }
      return;
    }

    byte[] combinedInputBuffer;
    try {
      combinedInputBuffer = GetCombinedInputBuffer(inputBuffer);
    } catch(ReaderBufferFullException) {
      throw;
    }

    try {
      UpdatePacketBufferAndQueue(combinedInputBuffer);
    } catch (PacketQueueFullException) {
      throw;
    }
  }

  private void HandleDataStartString(byte[] inputBuffer) {
    string inputStr = System.Text.Encoding.ASCII.GetString(inputBuffer);
    Logger.Debug($"Input string is '{inputStr}'");
    int matchIdx = inputStr.IndexOf(_DataStartString[_dataStartStrIdx]);
    if (matchIdx == -1) return;
    int i = matchIdx;
    do {
      if (inputStr[i] == _DataStartString[_dataStartStrIdx]) {
        i++;
        _dataStartStrIdx++;
      } else {
        return;
      }
    } while (i < inputStr.Length && _dataStartStrIdx < _DataStartString.Length);

    if (i == inputStr.Length && _dataStartStrIdx == _DataStartString.Length) {
      _waitingForDataStart = false;
      Logger.Debug("Read through data start packet!!!");
    } else if (i < inputStr.Length) {
      throw new InvalidDataStartPacketException($@"Start data packet contains
          both start string and data bytes: '{inputStr}'");
    }
  }

  private byte[] GetCombinedInputBuffer(byte[] inputBuffer) {
    if (_curInputBuffer.Length + inputBuffer.Length > _curInputBufferCapacity) {
      throw new ReaderBufferFullException(
        $"Serial reader buffer with capacity {_curInputBufferCapacity} is full."
      );
    }
    byte[] combinedInputBuffer = new byte[_curInputBuffer.Length +
                                              inputBuffer.Length];
    System.Buffer.BlockCopy(_curInputBuffer, 0, combinedInputBuffer,
                            0, _curInputBuffer.Length);
    System.Buffer.BlockCopy(inputBuffer, 0, combinedInputBuffer,
                            _curInputBuffer.Length, inputBuffer.Length);
    return combinedInputBuffer;
  }

  private void UpdatePacketBufferAndQueue(byte[] combinedInputBuffer) {
    if (combinedInputBuffer.Length > _PacketNumBytes) {
      byte[] packetBuffer = new byte[_PacketNumBytes];
      System.Buffer.BlockCopy(combinedInputBuffer, 0, packetBuffer,
                              0, packetBuffer.Length);
      _curInputBuffer = new byte[combinedInputBuffer.Length - _PacketNumBytes];
      System.Buffer.BlockCopy(combinedInputBuffer, _PacketNumBytes,
                              _curInputBuffer, 0, _curInputBuffer.Length);

      lock (_packetQueue) {
        Logger.Debug($@"UpdatePacketBufferAndQueue got lock and there are
            {_packetQueue.Count} entries in q");
        _packetQueue.Enqueue(packetBuffer);
        if (_packetQueue.Count >= _PacketQueueCapacity) {
          _ = _packetQueue.Dequeue();
          throw new PacketQueueFullException($@"Packet queue with capacity
            {_PacketQueueCapacity} reached limit. Dequeing 1st element in queue.");
        }
      }
    } else {
      _curInputBuffer = combinedInputBuffer;
    }
  }
}

