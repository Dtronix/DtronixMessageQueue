namespace DtronixMessageQueue.Tests.Performance
{
    public enum ServerMessageType : byte
    {
        Unset = 0,
        Ready = 1,
        Complete = 2,
        ThroughputTransfer = 3
    }
}