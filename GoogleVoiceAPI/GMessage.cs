using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleVoiceAPI
{
    public class GMessage
    {
        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonProperty("start_time")]
        public long MessageTime { get; set; }

        [JsonProperty("message_text")]
        public string Message { get; set; }

        [JsonProperty("conversation_id")]
        public string ConversationID { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("type")]
        public MessageType MessageType { get; set; }

        public bool SendMessage(string message)
        {
            return GSMSInstance.instance.SendMessage(this.PhoneNumber, message);
        }
    }

    public class GMessageGroup
    {
        public List<GMessage> call;
        public string group_id { get { return call[0].ConversationID; } }
    }

    public class GMessageCollection
    {
        public List<GMessageGroup> conversationgroup;
    }

    public enum MessageType
    {
        Sent = 11,
        Received = 10
    }
}
