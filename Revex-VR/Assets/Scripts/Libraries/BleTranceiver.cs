using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class BleTranceiver : Tranceiver {
  // This class and the corresponding DLL were taken from adabru's
  // DLL for the UWP BLE API (See https://github.com/adabru/BleWinrtDll)
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

  enum ConnectionStatus {
    SearchingDevices,
    SearchingServices,
    SearchingCharacteristics,
    Subscribing,
    CheckingSubscribeAttempts,
    CheckingForAwake,
  }

  public class CharacteristicInfo {
    public string Id { get; }
    public bool ShouldSubscribe { get; }
    public bool Subscribed { get; set; }
    public uint ExpectedSize { get; }

    public CharacteristicInfo(string id, bool shouldSubscribe, 
                                            uint expectedSize) {
      Id = id;
      ShouldSubscribe = shouldSubscribe;
      Subscribed = false;
      ExpectedSize = shouldSubscribe ? expectedSize : 0;
    }
  }

  public readonly string revexDeviceId = "BluetoothLE#BluetoothLE04:33:c2:80:5e:25-00:1e:c0:1d:42:8a";
  public readonly string revexServiceId = "{12345678-9012-3456-7890-1234567890ff}";
  public readonly string sensorCharacteristicId = "12345678-9012-3456-7890-123456789011";
  public readonly string hapticCharacteristicId = "12345678-9012-3456-7890-123456789022";
  public readonly string sleepCharacteristicId = "12345678-9012-3456-7890-123456789033";

  private readonly string _OkStatus = "Ok";
  private ConnectionStatus _status = ConnectionStatus.SearchingDevices;
  private Dictionary<string, CharacteristicInfo> _characteristics;
  private ConcurrentQueue<SensorSample> _sampleQueue =
                                          new ConcurrentQueue<SensorSample>();
  private CancellationTokenSource _readCts = new CancellationTokenSource();
  private bool _readThreadKilled = false;

  private const int _SleepStatusPacketNumBytes = 1;
  private const byte _AwakeVal = 1;
  private bool _deviceIsAwake = true; //false; TODO!!!!!!!!!!!!!!!!!

  public BleTranceiver() {
    _characteristics = new Dictionary<string, CharacteristicInfo>{
      {"sensor", new CharacteristicInfo(sensorCharacteristicId, 
                                        shouldSubscribe: true, 
                                        expectedSize: SensorSample.NumBytes) },
      {"haptic", new CharacteristicInfo(
            hapticCharacteristicId, shouldSubscribe: false, expectedSize: 0) },
      {"sleepStatus", new CharacteristicInfo(sleepCharacteristicId,
                                   shouldSubscribe: false,//true,  TODO!!!!!!!!!!!!!!!!!
                                   expectedSize: _SleepStatusPacketNumBytes) },
    };

    new Thread(ReadWorker).Start();
  }

  public override bool TryEstablishConnection() {
    // TODO(Issue 1): Catch and try to recover from thrown exceptions
    bool connectionEstablished = false;
    switch (_status) {
      case ConnectionStatus.SearchingDevices:
        if (ConnectToDevice())
          _status = ConnectionStatus.Subscribing;
        // TODO: If time allows, try to find out why we get stuck in
        //       State.PROCESSING for service/charac scanning.
        //_status = ConnectionStatus.SearchingServices;
        break;
      case ConnectionStatus.SearchingServices:
        if (ConnectToService())
          _status = ConnectionStatus.SearchingCharacteristics;
        break;
      case ConnectionStatus.SearchingCharacteristics:
        if (ConnectToCharacteristic())
          _status = ConnectionStatus.Subscribing;
        break;
      case ConnectionStatus.Subscribing:
        Subscribe();
        _status = ConnectionStatus.CheckingSubscribeAttempts;
        break;
      case ConnectionStatus.CheckingSubscribeAttempts:
        // TODO: remove and replace with below !!!!!!!!!!!!!!!!!!!!!!
        connectionEstablished = CheckSubscribeStatus();
        break;
      //  if (CheckSubscribeStatus())
      //    _status = ConnectionStatus.CheckingForAwake;
      //  break;
      //case ConnectionStatus.CheckingForAwake:
      //  connectionEstablished = DeviceIsAwake();
      //  break;
      default:
        throw new Exception(
            "Unknown case in BleTranceiver::TryEstablishConnection");
    }
    return connectionEstablished;
  }

  public override void CloseConnection() {
    _readCts.Cancel();
    Thread.Sleep(1000); // 1 sec
    Debug.Assert(_readThreadKilled);
    Impl.Quit();
    Logger.Debug("Closed BLE services.");
    _readCts.Dispose();
  }

  public override bool TryGetSensorData(out List<SensorSample> samples) {
    samples = new List<SensorSample>();
    while (_sampleQueue.TryDequeue(out SensorSample smpl)) samples.Add(smpl);
    return samples.Count > 0;
  }

  public override bool DeviceIsAwake() {
    return _deviceIsAwake;
  }

  public override void SendHapticFeedback(HapticFeedback feedback) {
    Impl.BLEData payload = new Impl.BLEData();
    payload.buf = new byte[1];
    payload.buf[0] = feedback.Payload;
    payload.size = 1;
    payload.deviceId = revexDeviceId;
    payload.serviceUuid = revexServiceId;
    payload.characteristicUuid = hapticCharacteristicId;
    // Block so that we can know whether write was successful.
    new Thread(() => {
      bool res = Impl.SendData(in payload, block: true);
      if (GetStatus() != _OkStatus || !res) {
        throw new BleException($"Ble.SendData failed: {GetStatus()}.");
      }
      Logger.Debug($"Sent haptic feedback packet.");
    }).Start();
  }

  private bool ConnectToDevice() {
    Impl.ScanStatus status;
    Impl.DeviceUpdate device = new Impl.DeviceUpdate();
    bool revexDeviceFound = false;

    Impl.StartDeviceScan();
    do {
      status = Impl.PollDevice(ref device, block: false);
      if (device.id == revexDeviceId) {
        Logger.Debug($"Connecting to revex device with id {device.id}");
        revexDeviceFound = true;
        Impl.StopDeviceScan();
      }
    } while (status == Impl.ScanStatus.AVAILABLE);

    if (GetStatus() != _OkStatus)
      throw new BleException($"ConnectToDevice failed: {GetStatus()}.");
    if (!revexDeviceFound && status == Impl.ScanStatus.FINISHED) {
      throw new NotFoundException(@"Device scan finished.
                                    Unable to find RevEx device.");
    }
    return revexDeviceFound;
  }

  private bool ConnectToService() {
    Impl.ScanStatus status;
    Impl.Service service = new Impl.Service();
    bool revexServiceFound = false;

    Impl.ScanServices(revexDeviceId);
    do {
      status = Impl.PollService(out service, block: false);
      if (service.uuid != "") Logger.Debug($"Revex service = {service.uuid}");
      if (service.uuid == revexServiceId) {
        revexServiceFound = true;
      }
    } while (status == Impl.ScanStatus.AVAILABLE);

    if (GetStatus() != _OkStatus)
      throw new BleException($"ConnectToService failed: {GetStatus()}.");
    if (!revexServiceFound && status == Impl.ScanStatus.FINISHED) {
      throw new NotFoundException(@"Services scan finished.
                                    Unable to find RevEx service.");
    }
    return revexServiceFound;
  }

  private bool ConnectToCharacteristic() {
    Impl.ScanStatus status;

    Impl.ScanCharacteristics(revexDeviceId, revexServiceId);
    foreach (KeyValuePair<string, CharacteristicInfo> characteristicInfo
                                                      in _characteristics) {
      Impl.Characteristic characteristic = new Impl.Characteristic();
      bool sensorCharacteristicFound = false;
      do {
        status = Impl.PollCharacteristic(out characteristic, block: false);
        if (characteristic.uuid != "")
          Logger.Debug($"Revex characteristic = {characteristic.uuid}");
        if (characteristic.uuid == characteristicInfo.Value.Id) {
          sensorCharacteristicFound = true;
        }
      } while (status == Impl.ScanStatus.AVAILABLE);

      if (GetStatus() != _OkStatus)
        throw new BleException($"ConnectToCharacteristic fail: {GetStatus()}.");

      if (!sensorCharacteristicFound) {
        if (status == Impl.ScanStatus.FINISHED) {
          throw new NotFoundException($@"Characteristics scan finished. 
                     Unable to find {characteristicInfo.Key} characteristic.");
        } else {
          return false;
        }
      }
    }
    return true;
  }

  private void Subscribe() {
    foreach (CharacteristicInfo characteristic in _characteristics.Values) {
      if (!characteristic.ShouldSubscribe) continue;

      new Thread(() => {
        bool res = Impl.SubscribeCharacteristic(revexDeviceId,
                                                revexServiceId,
                                                characteristic.Id,
                                                block: true);
        if (GetStatus() != _OkStatus || !res) {
          _status = ConnectionStatus.SearchingDevices;
          Logger.Error($"Ble.SubscribeCharacteristic failed: {GetStatus()}");
          //throw new BleException($@"Ble.SubscribeCharacteristic failed: 
          //                          {GetStatus()}.");
        } else {
          characteristic.Subscribed = true;
          Logger.Debug($"Successfully subscribed to characteristic.");
        }
      }).Start();
    }
  }

  private bool CheckSubscribeStatus() {
    foreach (CharacteristicInfo characteristic in _characteristics.Values) {
      if (!characteristic.ShouldSubscribe) continue;
      if (!characteristic.Subscribed) return false;
    }
    return true;
  }

  private void ReadWorker() {
    while (!_readCts.IsCancellationRequested) {
      while (Impl.PollData(out Impl.BLEData receivedData, block: false)) {
        Logger.Debug($"Received packet bytes = {BitConverter.ToString(receivedData.buf)}");
        if (GetStatus() != _OkStatus) {
          throw new BleException($"Ble.PollData failed: {GetStatus()}.");
        }

        switch (receivedData.size) {
          case SensorSample.NumBytes:
            byte[] sampleBuffer = new byte[receivedData.size];
            Buffer.BlockCopy(receivedData.buf, 0, sampleBuffer,
                                          0, receivedData.size);
            _sampleQueue.Enqueue(new SensorSample(sampleBuffer));
            break;
          case _SleepStatusPacketNumBytes:
            _deviceIsAwake = receivedData.buf[0] == _AwakeVal;
            break;
          default:
            Logger.Warning($"Unknown rx packet with size {receivedData.size}.");
            break;
        }
      }
      Thread.Sleep(90); // ms
    }
    _readThreadKilled = true;
  }

  private static string GetStatus() {
    Impl.ErrorMessage buf;
    Impl.GetError(out buf);
    return buf.msg;
  }
}
