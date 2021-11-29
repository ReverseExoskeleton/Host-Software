using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class BleTranceiver : Tranceiver {
  public class Impl {
    public enum ScanStatus { PROCESSING, AVAILABLE, FINISHED };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DeviceUpdate {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
      public string id;
      [MarshalAs(UnmanagedType.I1)]
      public bool isConnectable;
      [MarshalAs(UnmanagedType.I1)]
      public bool isConnectableUpdated;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
      public string name;
      [MarshalAs(UnmanagedType.I1)]
      public bool nameUpdated;
    }

    [DllImport("BleWinrtDll.dll", EntryPoint = "StartDeviceScan")]
    public static extern void StartDeviceScan();

    [DllImport("BleWinrtDll.dll", EntryPoint = "PollDevice")]
    public static extern ScanStatus PollDevice(ref DeviceUpdate device, bool block);

    [DllImport("BleWinrtDll.dll", EntryPoint = "StopDeviceScan")]
    public static extern void StopDeviceScan();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Service {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
      public string uuid;
    };

    [DllImport("BleWinrtDll.dll", EntryPoint = "ScanServices", CharSet = CharSet.Unicode)]
    public static extern void ScanServices(string deviceId);

    [DllImport("BleWinrtDll.dll", EntryPoint = "PollService")]
    public static extern ScanStatus PollService(out Service service, bool block);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Characteristic {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
      public string uuid;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
      public string userDescription;
    };

    [DllImport("BleWinrtDll.dll", EntryPoint = "ScanCharacteristics", CharSet = CharSet.Unicode)]
    public static extern void ScanCharacteristics(string deviceId, string serviceId);

    [DllImport("BleWinrtDll.dll", EntryPoint = "PollCharacteristic")]
    public static extern ScanStatus PollCharacteristic(out Characteristic characteristic, bool block);

    [DllImport("BleWinrtDll.dll", EntryPoint = "SubscribeCharacteristic", CharSet = CharSet.Unicode)]
    public static extern bool SubscribeCharacteristic(string deviceId, string serviceId, string characteristicId, bool block);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BLEData {
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
      public byte[] buf;
      [MarshalAs(UnmanagedType.I2)]
      public short size;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public string deviceId;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public string serviceUuid;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public string characteristicUuid;
    };

    [DllImport("BleWinrtDll.dll", EntryPoint = "PollData")]
    public static extern bool PollData(out BLEData data, bool block);

    [DllImport("BleWinrtDll.dll", EntryPoint = "SendData")]
    public static extern bool SendData(in BLEData data, bool block);

    [DllImport("BleWinrtDll.dll", EntryPoint = "Quit")]
    public static extern void Quit();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ErrorMessage {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
      public string msg;
    };

    [DllImport("BleWinrtDll.dll", EntryPoint = "GetError")]
    public static extern void GetError(out ErrorMessage buf);
  }

  public readonly string revexDeviceId = "BluetoothLE#BluetoothLE04:33:c2:80:5e:25-00:1e:c0:31:64:be";
  public readonly string revexServiceId = "12345678-9012-3456-7890-1234567890ff";
  public readonly string sensorCharacteristicId = "12345678-9012-3456-7890-123456789011";
  public readonly string hapticCharacteristicId = "12345678-9012-3456-7890-123456789022";

  private readonly string _OkStatus = "Ok";
  private Dictionary<string, string> _characteristicIds;
  private CancellationTokenSource _readCts = new CancellationTokenSource();
  private ConcurrentQueue<SensorSample> _sampleQueue =
                                          new ConcurrentQueue<SensorSample>();
  public BleTranceiver() {
    _characteristicIds = new Dictionary<string, string>{
      {"sensor", sensorCharacteristicId },
      {"haptic", hapticCharacteristicId },
    };
  }

  public override void EstablishConnection() {
    // TODO(Issue 1): Catch and try to recover from thrown exceptions
    ConnectToDevice(); 
    ConnectToService(); 
    ConnectToCharacteristic(); 
    Subscribe();

    Thread readThread = new Thread(ReadWorker);
  }

  public override void CloseConnection() {
    _readCts.Cancel();
    Impl.Quit();
    Thread.Sleep(1000); // 1 sec
    _readCts.Dispose();
  }

  public override bool TryGetSensorData(out List<SensorSample> samples) {
    samples = new List<SensorSample>();
    while (_sampleQueue.TryDequeue(out SensorSample smpl)) samples.Add(smpl);
    return samples.Count > 0;
  }

  public override void SendHapticFeedback(byte intensity) {
    Impl.BLEData payload = new Impl.BLEData();
    payload.buf = new byte[1];
    payload.buf[0] = intensity;
    payload.size = 1;
    payload.deviceId = revexDeviceId;
    payload.serviceUuid = revexServiceId;
    payload.characteristicUuid = hapticCharacteristicId;
    // TODO(Issue 2): May want to do this in a thread in case indefinite blocking
    // Block so that we can know whether write was successful.
    bool res = Impl.SendData(in payload,block: true);
    if (GetStatus() != _OkStatus || !res) {
      throw new BleException($"Ble.SendData failed: {GetStatus()}.");
    }
  }

  private void ConnectToDevice() {
    Impl.ScanStatus status;
    Impl.DeviceUpdate device = new Impl.DeviceUpdate();
    bool revexDeviceFound = false;

    Impl.StartDeviceScan();
    do {
      status = Impl.PollDevice(ref device, block: false);
      if (device.id == revexDeviceId) {
        revexDeviceFound = true;
        Impl.StopDeviceScan();
      }
    } while (!revexDeviceFound && status != Impl.ScanStatus.FINISHED);
    if (!revexDeviceFound) {
      throw new NotFoundException(@"Device scan finished.
                                    Unable to find RevEx device.");
    }
    if (GetStatus() != _OkStatus)
      throw new BleException($"ConnectToDevice failed: {GetStatus()}.");
  }

  private void ConnectToService() {
    Impl.ScanStatus status;
    bool revexServiceFound = false;

    Impl.ScanServices(revexDeviceId);
    do {
      status = Impl.PollService(out Impl.Service service, block: false);
      if (service.uuid == revexServiceId) {
        revexServiceFound = true;
      }
    } while (!revexServiceFound && status != Impl.ScanStatus.FINISHED);
    if (!revexServiceFound) {
      throw new NotFoundException(@"Services scan finished.
                                    Unable to find RevEx service.");
    }
    if (GetStatus() != _OkStatus)
      throw new BleException($"ConnectToService failed: {GetStatus()}.");
  }

  private void ConnectToCharacteristic() {
    Impl.ScanStatus status;

    Impl.ScanCharacteristics(revexDeviceId, revexServiceId);
    foreach (KeyValuePair<string, string> characteristicId in _characteristicIds) {
      bool sensorCharacteristicFound = false;
      do {
        status = Impl.PollCharacteristic(out Impl.Characteristic characteristic,
                                         block: false);
        if (characteristic.uuid == characteristicId.Value) {
          sensorCharacteristicFound = true;
        }
      } while (!sensorCharacteristicFound && status != Impl.ScanStatus.FINISHED);
      if (!sensorCharacteristicFound) {
        throw new NotFoundException($@"Characteristics scan finished. Unable to
                                  find {characteristicId.Key} characteristic.");
      }
    }
    if (GetStatus() != _OkStatus)
      throw new BleException($"ConnectToCharacteristic failed: {GetStatus()}.");
  }

  private void Subscribe() {
    // TODO(Issue 2): May want to do this in a thread in case indefinite blocking
    // Block so that we can know whether subscribe was successful.
    bool res = Impl.SubscribeCharacteristic(revexDeviceId, revexServiceId,
                                           sensorCharacteristicId, block: true);
    if (GetStatus() != _OkStatus || !res) {
      throw new BleException($@"Ble.SubscribeCharacteristic failed: 
                                {GetStatus()}.");
    }
  }

  private void ReadWorker() {
    while (!_readCts.IsCancellationRequested) {
      if (Impl.PollData(out Impl.BLEData receivedData, block: false)) {
        if (GetStatus() != _OkStatus) {
          throw new BleException($"Ble.PollData failed: {GetStatus()}.");
        }
        if (receivedData.size != SensorSample.NumBytes) {
          throw new InvalidSensorPacketException($@"Sensor packet contained 
             {receivedData.size} bytes. Expected {SensorSample.NumBytes}.");
        }
        _sampleQueue.Enqueue(new SensorSample(receivedData.buf));
      }
      Thread.Sleep(90); // ms
    }
  }

  private static string GetStatus() {
    Impl.ErrorMessage buf;
    Impl.GetError(out buf);
    return buf.msg;
  }
}

