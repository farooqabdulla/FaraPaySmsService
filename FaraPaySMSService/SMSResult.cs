using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaraPaySMSService
{
    internal class SMSResult
    {
        private SMS _sms = null;
        private bool _isSuccess = false;
        private string _jobNumber = string.Empty;
        private Int32 _statusId = 0;
        internal SMS sms { get { return this._sms; } set { this._sms = value; } }
        internal bool IsSuccess { get { return this._isSuccess; } set { this._isSuccess = value; } }
        internal string JobNumber { get { return this._jobNumber; } set { this._jobNumber = value; } }
        internal Int32 StatusId { get { return this._statusId; } set { this._statusId = value; } }
    }
}
