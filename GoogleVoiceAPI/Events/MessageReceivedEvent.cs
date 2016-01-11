using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleVoiceAPI.Events
{
    public class MessageReceivedEvent : EventArgs
    {
        private GMessage g;
        public MessageReceivedEvent(GMessage msg)
        {
            this.g = msg;
        }
        public GMessage Message { get { return this.g; } }
    }
}
