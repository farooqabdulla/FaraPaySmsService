using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaraPaySMSService
{
    internal class Label
    {
        internal const string LAST_ID = "@LastId";
        internal const string SUCCESS = "@Success";
        internal const string MESSAGE = "@Message";
        internal const string ID = "Id";
        internal const string MobileNumber = "MobileNumber";
        internal const string SenderId = "SenderId";
        internal const string Message = "Message";
        
        internal static class ProcedureParameters
        {
            internal const string SMS_ID = "@SMSId";
            internal const string STATUS_ID = "@StatusId";
            internal const string SMS_JOB_NUMBER = "@SMSJobNumber";
            internal const string SUCCESS = "@Success";
            internal const string MESSAGE = "@Message";
        }
    }
}
