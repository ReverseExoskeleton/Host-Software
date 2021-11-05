﻿using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class Controller : MonoBehaviour {
  // --------------- Communication ---------------
  public Tranceiver tranceiver;

  private bool _firstUpdate = true;
  private float _timeSinceLastPacketS = 0; // sec

  // --------------- Arm Estimation ---------------
  public Madgwick fusion;
  public Transform cubeTf;

  // -------------- Haptic Feedback --------------

  void Start() {
    tranceiver = new SerialReader();
    fusion = new Madgwick();
    tranceiver.EstablishConnection();
  }

  void Update() {
    if (tranceiver == null) return;
    if (_firstUpdate) {
      Logger.Debug("Update tid =" + Thread.CurrentThread.ManagedThreadId);
      _firstUpdate = false;
    }

    _timeSinceLastPacketS += Time.deltaTime;
    try {
      List<SensorSample> samples = tranceiver.GetSensorData();
      float samplePeriod = _timeSinceLastPacketS / samples.Count;
      _timeSinceLastPacketS = 0;

      foreach (SensorSample sample in samples) {
        fusion.AhrsUpdate(sample.Imu.AngVel, sample.Imu.LinAccel,
                          sample.Imu.MagField, samplePeriod);
      }
      cubeTf.rotation = fusion.GetQuaternion();
      Vector3 eulerAng = fusion.GetEulerAngles();
      Logger.Testing($"roll={eulerAng.x}, pitch={eulerAng.y}, yaw={eulerAng.z}");
      // --------------- TODO: Remove this later after debugging ------------
      //Madgwick.Impl.FusionQuaternion q = fusion.GetQuaternion();
      //tf.rotation = new Quaternion(q.x, q.z, q.y, q.w);
      //Madgwick.Impl.FusionEulerAngles e = fusion.GetEulerAngles();
      //Logger.Testing($"roll={e.pitch}, pitch={e.roll}, yaw={e.yaw}");
      // --------------------------------------------------------------------
    } catch (PacketQueueEmptyException e) {
      Logger.Warning(e);
    }
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}
