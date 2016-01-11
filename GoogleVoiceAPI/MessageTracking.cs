using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleVoiceAPI
{
    class MessageTracking
    {
        public List<MessageTrackingMessage> Messages = new List<MessageTrackingMessage>();

        public List<GMessage> FindUnreadMessages(List<GMessage> s)
        {
            List<GMessage> NotInList = new List<GMessage>();
            foreach (GMessage gm in s)
            {
                bool matches = false;
                foreach (MessageTrackingMessage m in this.Messages)
                {
                    if (gm.ConversationID == m.ConversationID && gm.ID == m.ID)
                    {
                        matches = true;
                    }
                }
                if (!matches)
                {
                    this.Messages.Add(new MessageTrackingMessage(gm.ConversationID, gm.ID));
                    NotInList.Add(gm);
                }
            }
            return NotInList;
        }

        public void AddMessages(List<GMessage> s)
        {
            foreach (GMessage gm in s)
            {
                this.Messages.Add(new MessageTrackingMessage(gm.ConversationID, gm.ID));
            }
        }
    }

    class MessageTrackingMessage
    {
        public MessageTrackingMessage(string cid, string id)
        {
            this.ConversationID = cid;
            this.ID = id;
        }
        public string ConversationID;
        public string ID;
    }
}
