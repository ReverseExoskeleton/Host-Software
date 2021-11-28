public class MovingAvg {
  private float _alpha;
  private float _state;
  public MovingAvg(float alpha = 0.8F) {
    _alpha = alpha; 
    _state = 0;
  }

  public float Update(float x) {
    _state = (_alpha * x) + ((1 - _alpha) * _state);
    return _state;
  }

  public float Current() {
    return _state;
  }
}
