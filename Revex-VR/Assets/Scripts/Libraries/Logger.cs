//#define ENABLE_LOGS
// Determine why this doesn't work
// https://docs.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2008/4xssyw96(v=vs.90)?redirectedfrom=MSDN

public static class Logger {
  private static bool _EnableDebug = true;
  private static bool _EnableTest = false;
  private static bool _EnableWarn = true;
  private static bool _EnableError = true;

  public static void Debug(object logMsg) {
    if (_EnableDebug) UnityEngine.Debug.Log(logMsg);
  }

  public static void Testing(object logMsg) {
    if (_EnableTest) UnityEngine.Debug.Log(logMsg);
  }

  public static void Warning(object logMsg) {
    if (_EnableWarn) UnityEngine.Debug.LogWarning(logMsg);
  }

  public static void Error(object logMsg) {
    if (_EnableError) UnityEngine.Debug.LogError(logMsg);
  }
}