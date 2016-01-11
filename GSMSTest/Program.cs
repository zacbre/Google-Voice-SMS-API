using System;
using System.Collections.Generic;
using System.Text;
using GoogleVoiceAPI;
using System.IO;

namespace GSMSTest
{
    class Program
    {
        static void Main(string[] args)
        {
            GSMS s = new GSMS("yourgmailusernamehere", "yourpasswordhere");
            if (s.Login())
            {
                Console.Title = s.Account.PhoneNumber;
                s.OnMessageReceived += s_OnMessageReceived;
                s.StartReceiveMessages();
            }
            while (true)
            {
                System.Threading.Thread.Sleep(2000);
            }
        }

        static void s_OnMessageReceived(object sender, GoogleVoiceAPI.Events.MessageReceivedEvent e)
        {
            Console.WriteLine("Message Received From {0}: {1}", e.Message.PhoneNumber, e.Message.Message);
        }
    }
}
