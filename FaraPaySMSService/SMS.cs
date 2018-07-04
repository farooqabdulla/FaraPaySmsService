using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaraPaySMSService
{
    internal class SMS
    {
        int id = 0;
        string _mobileNumber = String.Empty;
        string _message = String.Empty;
        string _senderId = String.Empty;
        string _messageType = String.Empty;
        string _dbName = String.Empty;
        bool _isSuccess = false;
        string _jobNumber = "";
        public int Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        public string MobileNumber
        {
            get
            {
                return this._mobileNumber;
            }
            set
            {
                this._mobileNumber = value;
            }
        }

        public string Message
        {
            get
            {
                return this._message;
            }
            set
            {
                this._message = value;
            }
        }

        public string SenderId
        {
            get
            {
                return this._senderId;
            }
            set
            {
                this._senderId = value;
            }
        }

        public string MessageType
        {
            get
            {
                return this._messageType;
            }
            set
            {
                this._messageType = value;
            }
        }

        public string DbName
        {
            get
            {
                return this._dbName;
            }
            set
            {
                this._dbName = value;
            }
        }

        public bool IsSuccess
        {
            get
            {
                return this._isSuccess;
            }
            set
            {
                this._isSuccess = value;
            }
        }

        public string JobNumber
        {
            get
            {
                return this._jobNumber;
            }
            set
            {
                this._jobNumber = value;
            }
        }

    }
}
