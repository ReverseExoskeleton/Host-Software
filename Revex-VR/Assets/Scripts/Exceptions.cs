using System;


public class HardwareConfigurationException : Exception {
  public HardwareConfigurationException() { }
  public HardwareConfigurationException(string message) : base(message) { }
  public HardwareConfigurationException(string message, Exception inner) :
    base(message, inner) { }
}

public class InvalidDataStartPacketException  : Exception {
  public InvalidDataStartPacketException () { }
  public InvalidDataStartPacketException (string message) : base(message) { }
  public InvalidDataStartPacketException (string message, Exception inner) :
    base(message, inner) { }
}

public class ReaderBufferFullException : Exception {
  public ReaderBufferFullException() { }
  public ReaderBufferFullException(string message) : base(message) { }
  public ReaderBufferFullException(string message, Exception inner) :
    base(message, inner) { }
}

public class PacketQueueFullException : Exception {
  public PacketQueueFullException() { }
  public PacketQueueFullException(string message) : base(message) { }
  public PacketQueueFullException(string message, Exception inner) :
    base(message, inner) { }
}

public class FailedToCloseException : Exception {
  public FailedToCloseException() { }
  public FailedToCloseException(string message) : base(message) { }
  public FailedToCloseException(string message, Exception inner) :
    base(message, inner) { }
}

public class BleException : Exception {
  public BleException() { }
  public BleException(string message) : base(message) { }
  public BleException(string message, Exception inner) :
    base(message, inner) { }
}


public class InvalidSensorPacketException : Exception {
  public InvalidSensorPacketException() { }
  public InvalidSensorPacketException(string message) : base(message) { }
  public InvalidSensorPacketException(string message, Exception inner) :
    base(message, inner) { }
}

public class NotFoundException : Exception {
  public NotFoundException() { }
  public NotFoundException(string message) : base(message) { }
  public NotFoundException(string message, Exception inner) :
    base(message, inner) { }
}

