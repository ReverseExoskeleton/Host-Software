using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Vector3D = Madgwick.Impl.FusionVector3;

public class ValueDisplay : MonoBehaviour {
  public Text xText;
  public Text yText;
  public Text zText;
  private string _x;
  private string _y;
  private string _z;

  // Start is called before the first frame update
  void Start() {}

  // Update is called once per frame
  void Update() {
    if (xText.text != _x) {
      xText.text = _x;
    }
    if (yText.text != _y) {
      yText.text = _y;
    }
    if (zText.text != _z) {
      zText.text = _z;
    }
  }

  public void UpdateValue(Vector3D sample) {
    _x = sample.x.ToString("0.000");
    _y = sample.y.ToString("0.000");
    _z = sample.z.ToString("0.000");
  }
}
