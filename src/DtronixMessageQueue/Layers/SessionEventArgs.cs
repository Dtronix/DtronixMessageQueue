using System;

namespace DtronixMessageQueue.Layers
{
    public class SessionEventArgs : EventArgs
    {
        public SessionEventArgs(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; set; }
    }
}
