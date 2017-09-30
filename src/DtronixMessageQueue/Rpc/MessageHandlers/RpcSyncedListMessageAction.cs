namespace DtronixMessageQueue.Rpc.MessageHandlers
{
    /// <summary>
    /// Type of message which is being sent.
    /// </summary>
    public enum RpcSyncedListMessageAction : byte
    {
        /// <summary>
        /// Unknown default type.
        /// </summary>
        Unset = 0,


        Create = 1,
        Delete = 2,

        Add = 3,
        Remove = 4,
        Clear = 5,
    }
}