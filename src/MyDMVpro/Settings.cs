using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro
{
    public class AutoIMSSettings
    {
        public string TriggerUrl { get; set; }
        public string MergeCRHttpUrl { get; set; }
    }
    public class SmtpSettings
    {
        public string From { get; set; }
        public string FromDisplayName { get; set; }
        public string EnableSsl { get; set; }
        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }
        public string BccAddress { get; set; }
    }
}
