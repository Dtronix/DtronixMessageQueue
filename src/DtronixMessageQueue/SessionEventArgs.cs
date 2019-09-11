using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue
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
