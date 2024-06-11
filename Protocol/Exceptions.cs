

namespace Protocol
{
    public abstract class ProtocolException : System.Exception
    {
        public ProtocolException(string message) : base(message) { }
    }

    public class UnexpectedClientBehaviorExecption : ProtocolException
    {
        public UnexpectedClientBehaviorExecption(string msg) : base(msg) { }
    }

    internal class DataRecvTimeoutException : UnexpectedClientBehaviorExecption
    {
        public DataRecvTimeoutException() : base("A timeout occurred while attempting to recv data.") { }
    }

    internal class UnexpectedValueException : UnexpectedClientBehaviorExecption
    {
        public UnexpectedValueException(string name) : base($"Value {name} is out of range.") { }
    }

    internal class UnexpectedPacketException : UnexpectedClientBehaviorExecption
    {
        public UnexpectedPacketException() : base("Encountered an unexpected packet.") { }
    }

    internal class InvalidEncodingException : UnexpectedClientBehaviorExecption
    {
        public InvalidEncodingException() : base("Failed to decode the data due to invalid encoding.") { }
    }

    internal class BufferOverflowException : UnexpectedClientBehaviorExecption
    {
        public BufferOverflowException() : base("Unexpected buffer overflow occurred due to excessive data.") { }
    }

    internal class EmptyBufferException : UnexpectedClientBehaviorExecption
    {
        public EmptyBufferException() : base("Attempting to read from an empty buffer.") { }
    }

    internal class TeleportationConfirmTimeoutException : UnexpectedClientBehaviorExecption
    {
        public TeleportationConfirmTimeoutException() : base("TODO: Add description.") { }
    }

    internal class ResponseKeepAliveTimeoutException : UnexpectedClientBehaviorExecption
    {
        public ResponseKeepAliveTimeoutException() : base("TODO: Add description.") { }
    }

    internal class KeepaliveTimeoutException : UnexpectedClientBehaviorExecption
    {
        public KeepaliveTimeoutException() : base("TODO: Add description.") { }
    }

    public class DisconnectedClientException : ProtocolException
    {
        public DisconnectedClientException() : base("The connection with the client has been terminated.") { }
    }

    public class TryAgainException : ProtocolException
    {
        public TryAgainException() : base("No data is waiting to be received.") { }
    }

}
