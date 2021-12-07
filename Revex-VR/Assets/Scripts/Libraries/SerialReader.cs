using System;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Collections.Concurrent;


public class SerialReader : Tranceiver {
  private const int _curInputBufferCapacity = 50; // Arbitrary
  // TODO: May also want to send message back to IMU once received so then it
  // can start sending data packets
  private const string _DataStartString = "Here comes the data bitch"; // Arbitrary

  private SerialPort _serialPort;
  private byte[] _curInputBuffer = new byte[0];
  private ConcurrentQueue<SensorSample> _sampleQueue =
                                         new ConcurrentQueue<SensorSample>();
  private bool _dataStartReceived = false;
  private int _dataStartStrIdx = 0;
  private bool _isRunning = true;
  private bool _finished = false;

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

    StartReading();
  }

  public override bool TryEstablishConnection() {
    return _dataStartReceived; 
  }

  public override void CloseConnection() {
    _isRunning = false;
    Thread.Sleep(1000); // ms
    if (!_finished)
      throw new FailedToCloseException($"Failed to close serial port.");
    Logger.Debug("Successfully closed serial port.");
  }

  public override bool TryGetSensorData(out List<SensorSample> samples) {
    samples = new List<SensorSample>();
    while (_sampleQueue.TryDequeue(out SensorSample smpl)) samples.Add(smpl);
    return samples.Count > 0;
  }

  public override void SendHapticFeedback(HapticFeedback feedback) {
    throw new NotImplementedException();
  }

  private void StartReading() {
    // Idea from https://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
    byte[] buffer = new byte[SensorSample.NumBytes];
    void _kickoffRead() {
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
        } catch (Exception e) {
          Logger.Error("Unknow Exception in kickoffRead: " + e);
        }

        if (_isRunning) {
          _kickoffRead();
        } else {
          _serialPort.Close();
          _finished = true;
        }
      }, null);
    }
    _kickoffRead();
  }

  private void HandleSerialDataEvent(byte[] inputBuffer) {
    if (!_dataStartReceived) {
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

    UpdatePacketBufferAndQueue(combinedInputBuffer);
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
      _dataStartReceived = true;
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
    if (combinedInputBuffer.Length > SensorSample.NumBytes) {
      byte[] packetBuffer = new byte[SensorSample.NumBytes];
      System.Buffer.BlockCopy(combinedInputBuffer, 0, packetBuffer,
                              0, packetBuffer.Length);
      _curInputBuffer = new byte[combinedInputBuffer.Length - SensorSample.NumBytes];
      System.Buffer.BlockCopy(combinedInputBuffer, SensorSample.NumBytes,
                              _curInputBuffer, 0, _curInputBuffer.Length);
      
      _sampleQueue.Enqueue(new SensorSample(packetBuffer));
    } else {
      _curInputBuffer = combinedInputBuffer;
    }
  }
}

