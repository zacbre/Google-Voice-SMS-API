using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleVoiceAPI
{
    public class AccountSettings
    {
        [JsonProperty("phone_number")]
        public string PhoneNumber{ get; set; }
    }

    public class GSMSInstance
    {
        public static GSMS instance;
    }
}
