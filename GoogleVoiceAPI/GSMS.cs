using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using System.Collections.Specialized;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using GoogleVoiceAPI.Events;

namespace GoogleVoiceAPI
{
    public class GSMS
    {
        private string email;
        private string password;
        private CookieContainer cookie;

        private string init_url = "https://www.google.com/voice/m";
        private string auth1_url = "https://accounts.google.com/ServiceLogin?service=grandcentral&passive=1209600&continue=https://www.google.com/voice/m?initialauth&followup=https://www.google.com/voice/m?initialauth";
        private string action_url;
        private string gvx;

        private AccountSettings acc;

        private bool receive = false;

        private WebClientEx wc;
        private MessageTracking Tracking;

        public event EventHandler<MessageReceivedEvent> OnMessageReceived = delegate { };
        public AccountSettings Account { get { return acc; } }
        
        public GSMS(string email, string password)
        {
            this.email = email;
            this.password = password;
            cookie = new CookieContainer();
            wc = new WebClientEx(cookie) { Proxy = null };
        }

        public bool Login()
        {
            NameValueCollection fields = this.ExtractFormFields(auth1_url);
            if (fields == null) return false;
            fields["Email"] = this.email;
            string auth2_url = this.action_url;

            string ret = this.POST(auth2_url, fields, this.auth1_url);
            if (ret == null) return false;

            fields = this.ExtractFormFields(ret, true);
            if (fields == null) return false;
            fields["Passwd"] = this.password;
            string auth3_url = this.action_url;

            ret = this.POST(auth3_url, fields, auth2_url);

            this.gvx = this.GetGVX();

            this.GetAccountInformation();
            GSMSInstance.instance = this;

            return true;
        }

        public bool SendMessage(string phone, string message)
        {
            string send_url = string.Format("{0}/x?m=sms&n={1}&txt={2}&v=13", this.init_url, phone, Uri.EscapeDataString(message));
            string output = this.POST(send_url, this.get_gvx(), this.init_url);
            return (output != null && output.Length > 0);
        }

        public bool MarkAsRead(string conv_id)
        {
            string send_url = string.Format("{0}/x?m=mod&id={1}&rm=unread&v=13", this.init_url, conv_id);
            string output = this.POST(send_url, this.get_gvx(), this.init_url);
            return (output != null && output.Length > 0);
        }

        public bool DeleteConversation(string conv_id)
        {
            string send_url = string.Format("{0}/x?m=mod&id={1}&add=trash&v=13", this.init_url, conv_id);
            string output = this.POST(send_url, this.get_gvx(), this.init_url);
            return (output != null && output.Length > 0);
        }

        private void GetAccountInformation()
        {
            try
            {
                string send_url = string.Format("{0}/x?m=init&v=13", this.init_url);
                string output = this.POST(send_url, this.get_gvx(), this.init_url);
                output = output.Replace(")]}',\n", "");
                JObject l = JObject.Parse(output);
                JsonSerializer ser = new JsonSerializer();
                JToken s = l["settings_response"]["did_info"][0];
                this.acc = (AccountSettings)JsonConvert.DeserializeObject<AccountSettings>(s.ToString());
            }
            catch { }
        }

        public GMessageCollection GetAllMessages() 
        {
            try {
                string send_url = string.Format("{0}/x?m=init&v=13", this.init_url);
                string output = this.POST(send_url, this.get_gvx(), this.init_url);
                output = output.Replace(")]}',\n", "");
                JObject l = JObject.Parse(output);
                JsonSerializer ser = new JsonSerializer();
                GMessageCollection g = (GMessageCollection)ser.Deserialize(new JTokenReader(l["conversations_response"]), typeof(GMessageCollection));
                return g;
            }
            catch
            { 
                return null;
            }
        }

        public void StartReceiveMessages()
        {
            receive = true;
            new Thread(delegate()
            {
                while (receive)
                {
                    this.RefreshMessages();
                    Thread.Sleep(5000);
                }
            }).Start();
        }

        public void StopReceiveMessages()
        {
            receive = false;
        }

        private void RefreshMessages()
        {
            GMessageCollection s = this.GetAllMessages();
            if (Tracking == null)
            {
                Tracking = new MessageTracking();
                foreach (GMessageGroup w in s.conversationgroup)
                {
                    Tracking.AddMessages(w.call);
                }
                return;
            }
            foreach (GMessageGroup w in s.conversationgroup)
            {
                List<GMessage> gmsg = Tracking.FindUnreadMessages(w.call);
                if (gmsg.Count > 0)
                {
                    foreach(GMessage x in gmsg) {
                        if (x.MessageType == MessageType.Received)
                            this.OnMessageReceived.Invoke(this, new MessageReceivedEvent(x));
                    }
                }
            }            
        }

        private string get_gvx()
        {
            return "{ \"gvx\": \"" + this.gvx + "\" }";
        }

        public NameValueCollection ExtractFormFields(string form, bool html = false)
        {
            try
            {
                string form_html;
                if (!html)
                {
                    form_html = this.wc.DownloadString(form);
                }
                else
                {
                    form_html = form;
                }
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(form_html);
                HtmlNode formfield = doc.DocumentNode.SelectSingleNode("//form");
                this.action_url = formfield.Attributes["action"].Value;
                //get fields.
                NameValueCollection fields = new NameValueCollection();
                foreach (HtmlNode input in formfield.SelectNodes("//input"))
                {
                    try
                    {
                        if (!input.Attributes.Contains("name")) continue;
                        fields.Add(input.Attributes["name"].Value, (input.Attributes.Contains("value") ? input.Attributes["value"].Value : ""));
                    }
                    catch { }
                }
                return fields;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private string GetGVX()
        {
            CookieCollection w = cookie.GetCookies(new Uri("https://www.google.com/voice"));
            foreach (Cookie p in w)
            {
                if (p.Name == "gvx") return p.Value;
            }
            return "";
        }

        private string GET(string url) {
            this.wc.Headers.Add("User-Agent: Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_0 like Mac OS X; en-us) AppleWebKit/532.9 (KHTML, like Gecko) Version/4.0.5 Mobile/8A293 Safari/6531.22.7");
            try
            {
                return this.wc.DownloadString(url);
            }
            catch {
                return null;
            }
        }

        private string POST(string url, NameValueCollection values, string referrer = "")
        {
            this.wc.Headers.Add("User-Agent: Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_0 like Mac OS X; en-us) AppleWebKit/532.9 (KHTML, like Gecko) Version/4.0.5 Mobile/8A293 Safari/6531.22.7");
            if (referrer != "")
            {
                this.wc.Headers.Add("Referer: " + referrer);
            }
            try
            {
                return Encoding.ASCII.GetString(this.wc.UploadValues(url, values));
            }
            catch { return null; }
        }

        private string POST(string url, string fields, string referrer = "")
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Referer = referrer;
            httpWebRequest.ContentLength = fields.Length;
            httpWebRequest.CookieContainer = cookie;
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(fields);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }
    }
}
