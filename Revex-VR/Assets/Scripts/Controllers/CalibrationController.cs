using UnityEngine;
using System.Collections.Generic;

class CalibrationController : MonoBehaviour {
  // --------------- Communication ---------------
  public Tranceiver tranceiver;
  bool connected = false;
  public bool useBleTranceiver = true;

  // --------------- Magnetometer Calibration ---------------
  private Vector3 _min = Vector3.positiveInfinity;
  private Vector3 _max = Vector3.negativeInfinity;


  void Start() {
    if (useBleTranceiver) {
      tranceiver = new BleTranceiver();
    } else {
      tranceiver = new SerialReader();
    }
  }

  void Update() {
    if (!connected) {
      connected = tranceiver.TryEstablishConnection();
    } else {
      List<SensorSample> samples = new List<SensorSample>();
      if (tranceiver?.TryGetSensorData(out samples) == false) return;

      foreach (SensorSample sample in samples) {
        PrintMagnetometerBias(sample.Imu.MagField);
      }
    }
  }

  private void PrintMagnetometerBias(Vector3 rawMag) {
    Logger.Testing("----------------- Hard-Iron Bias -----------------");

    _min = Vector3.Min(rawMag, _min);
    _max = Vector3.Max(rawMag, _max);

    Vector3 mid = (_max + _min) / 2;
    Logger.Testing($"Hard-Iron bias = ({mid.x}, {mid.y}, {mid.y})");


    // Spin the board around until you see the field vector elements
    // settle close to each other and range from 25uT to 65uT. 
    // Then use the hard-iron bias printed out.
    Vector3 field = (_max - _min) / 2;
    Logger.Testing($"Field = ({field.x}, {field.y}, {field.y})");
    Logger.Testing("--------------------------------------------------");
  }

  private void OnApplicationQuit() {
    tranceiver?.Dispose();
  }
}

