namespace DtronixMessageQueue
{
    /// <summary>
    /// Enum used to determine what type a frame is and how to handle it.
    /// </summary>
    public enum MqFrameType : byte
    {
        /// <summary>
        /// This frame type has not been defined.
        /// </summary>
        Unset = 0,

        /// <summary>
        /// This frame type has not been determined yet.
        /// </summary>
        Empty = 1,

        /// <summary>
        /// This frame is part of a larger message.
        /// </summary>
        More = 2,

        /// <summary>
        /// This frame is the last part of a message.
        /// </summary>
        Last = 3,

        /// <summary>
        /// This frame is an empty frame and the last part of a message.
        /// </summary>
        EmptyLast = 4,

        /// <summary>
        /// This frame is a command to the MQ server or client to be processed and consumed internally.
        /// </summary>
        Command = 5,

        /// <summary>
        /// This frame is a type of command frame with no body.  This frame is consumed by the FrameBuilder.
        /// </summary>
        Ping = 6
    }
}