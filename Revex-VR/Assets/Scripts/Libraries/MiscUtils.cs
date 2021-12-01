public readonly struct HapticFeedbackPercents {
  public float DutyCycle { get; }
  public float Frequency { get; }

  public HapticFeedbackPercents(float dutyCycle, float frequency) {
    DutyCycle = dutyCycle;
    Frequency = frequency;
  }
}

