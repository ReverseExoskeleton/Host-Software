using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Madgwick {
  // dll api
  public class Impl {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionVector3 {
      [MarshalAs(UnmanagedType.R4)]
      public float x;
      [MarshalAs(UnmanagedType.R4)]
      public float y;
      [MarshalAs(UnmanagedType.R4)]
      public float z;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FusionBias {
      [MarshalAs(UnmanagedType.R4)]
      public float threshold;
      [MarshalAs(UnmanagedType.R4)]
      public float samplePeriod;
      [MarshalAs(UnmanagedType.R4)]
      public float filterCoefficient;
      [MarshalAs(UnmanagedType.R4)]
      public float stationaryTimer;
      [MarshalAs(UnmanagedType.Struct)]
      public FusionVector3 gyroscopeBias;
    }

    [DllImport("MadgwickDll.dll", EntryPoint = "FusionBiasInitialise", CharSet = CharSet.Unicode)]
    public static extern void FusionBiasInitialise(out FusionBias fusionBias,
                                                   float threshold, 
                                                   float samplePeriod);
  }

}
