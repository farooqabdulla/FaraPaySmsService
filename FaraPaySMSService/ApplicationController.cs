using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Net;
using System.IO;
using Microsoft.VisualBasic;


namespace FaraPaySMSService
{
    internal class ApplicationController
    {
        private Thread _pollThread = null;
        private Thread[] _pushThreads = null;
        private Thread _statusUpdateThread = null;
        private Queue<SMS> _smsQueue = new Queue<SMS>();
        private Queue<SMSResult> _smsStatusQueue = new Queue<SMSResult>();
        private Mutex _smsQueueMutex = new Mutex();
        internal ApplicationController()
        {
            this.LoadConfig();
        }

        internal void Start()
        {
            this._pollThread = new Thread(new ThreadStart(this.GetPendingSMSes));
            this._pollThread.Name = "Poller";
            this._pollThread.Start();
            this._pushThreads = new Thread[SharedClass.PushThreads];
            for (byte index = 1; index <= SharedClass.PushThreads; index++)
            {
                this._pushThreads[index - 1] = new Thread(new ThreadStart(this.Push));
                this._pushThreads[index - 1].Name = "Push_" + index.ToString();
                this._pushThreads[index - 1].Start();
            }
            this._statusUpdateThread = new Thread(new ThreadStart(this.UpdateSMSStatus));
            this._statusUpdateThread.Name = "StatusUpdate";
            this._statusUpdateThread.Start();
        }

        internal void Stop()
        {
            while (this._pollThread.ThreadState != ThreadState.Stopped)
            {
                if (this._pollThread.ThreadState == ThreadState.WaitSleepJoin)
                    this._pollThread.Interrupt();
                SharedClass.Logger.Info("Poll Thread Not Stopped Yet");
                Thread.Sleep(1000);
            }
            
            while (this._statusUpdateThread.ThreadState != ThreadState.Stopped)
            {
                if (this._statusUpdateThread.ThreadState == ThreadState.WaitSleepJoin)
                    this._statusUpdateThread.Interrupt();
                SharedClass.Logger.Info("Status Update Thread Not Stopped Yet");
                Thread.Sleep(1000);
            }
            SharedClass.IsServiceCleaned = true;
        }

        private void GetPendingSMSes()
        {
            SharedClass.Logger.Info("Started");
            SqlConnection sqlConnection = new SqlConnection(SharedClass.ConnectionString);
            SqlCommand sqlCommand = new SqlCommand("GetPendingSMSes", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            SqlDataAdapter da = null;
            DataSet ds = null;
            long lastId = 0;
            while (!SharedClass.HasStopSignal)
            {
                try
                {
                    sqlCommand.Parameters.Clear();
                    sqlCommand.Parameters.Add(Label.LAST_ID, SqlDbType.BigInt).Value = lastId;
                    sqlCommand.Parameters.Add(Label.SUCCESS, SqlDbType.Bit).Direction = ParameterDirection.Output;
                    sqlCommand.Parameters.Add(Label.MESSAGE, SqlDbType.VarChar, 1000).Direction = ParameterDirection.Output;
                    da = new SqlDataAdapter();
                    da.SelectCommand = sqlCommand;
                    da.Fill(ds = new DataSet());
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            SMS sms = new SMS();
                            sms.Id = Convert.ToInt32(row["Id"].ToString());
                            if (!string.IsNullOrEmpty(row[Label.MobileNumber].ToString()))
                                sms.MobileNumber = row[Label.MobileNumber].ToString();
                            if (!string.IsNullOrEmpty(row[Label.SenderId].ToString()))
                                sms.SenderId = row[Label.SenderId].ToString();
                            if (!string.IsNullOrEmpty(row[Label.Message].ToString()))
                                sms.Message = row[Label.Message].ToString();
                            this.EnQueueSMS(sms);
                            lastId = sms.Id;
                        }
                    }
                    Thread.Sleep(5000);
                }
                catch (ThreadAbortException e)
                { }
                catch (ThreadInterruptedException e)
                { }
                catch (Exception e)
                {
                    SharedClass.Logger.Error(e.ToString());
                }
                finally
                {
                    da = null;
                    ds = null;
                }
            }
            SharedClass.Logger.Info("Exit");
        }


        private void Push()
        {
            SharedClass.Logger.Info("Started");
            string smsResponse = string.Empty;
            while (!SharedClass.HasStopSignal)
            {
                SMSResult smsResult = null;
                smsResult = new SMSResult();
                
                try
                {
                    if (this.SMSQueueCount() > 0)
                    {
                        SMS sms = this.DeQueueSMS();
                        smsResult.sms = sms;
                        JObject Jobj = new JObject();
                        if (sms != null)
                        {
                            JObject smsPostdata;
                            sms.MobileNumber = sms.MobileNumber.Replace(" ","").Replace("\n","").Replace("\r", "").Replace("+", "");
                            HttpWebRequest webRequestObj = null;
                            HttpWebResponse webResponseObj = null;
                            StreamWriter streamWriterObj = null;
                            StreamReader streamReaderObj = null;
                            CredentialCache credentials = new CredentialCache();
                            
                            //if ((IsUnicode(sms.Message) == true)) {
                            //    sms.MessageType = "OL";
                            //}
                            //else {
                            //    sms.MessageType = "N";
                            //}

                            //if ((sms.MessageType == "OL")) {
                            //    sms.Message = convertHexa(sms.Message);
                            //}
                            
                            smsPostdata = new JObject(
                                new JProperty("Text", sms.Message), 
                                new JProperty("Number", sms.MobileNumber), 
                                new JProperty("DRNotifyUrl", ""), 
                                new JProperty("DRNotifyHttpMethod", "POST"), 
                                new JProperty("Tool", "API"), 
                                new JProperty("SenderId", sms.SenderId));
                            SharedClass.Logger.Info("PostData" + smsPostdata.ToString());
                            credentials.Add(new Uri(SharedClass.SMSProvider), 
                                "Basic", new NetworkCredential(SharedClass.SMSProviderAuthkey, SharedClass.SMSProviderSecretKey));
                            webRequestObj = (HttpWebRequest) WebRequest.Create(SharedClass.SMSProvider);
                            webRequestObj.Method = "Post";
                            webRequestObj.Credentials = credentials;
                            webRequestObj.ContentType = "application/json";
                            streamWriterObj = new StreamWriter(webRequestObj.GetRequestStream());
                            streamWriterObj.Write(smsPostdata);
                            streamWriterObj.Flush();
                            streamWriterObj.Close();
                            webResponseObj =(HttpWebResponse) webRequestObj.GetResponse();
                            streamReaderObj = new StreamReader(webResponseObj.GetResponseStream());
                            smsResponse = streamReaderObj.ReadToEnd();
                            streamReaderObj.Close();
                            
                            Jobj = JObject.Parse(smsResponse);
                            
                            sms.JobNumber = Jobj.SelectToken("ApiId").ToString();
                            if (Convert.ToBoolean(Jobj.SelectToken("Success").ToString())) {
                                smsResult.IsSuccess = true;
                                smsResult.StatusId = 3;
                                smsResult.JobNumber = sms.JobNumber;
                            }
                            else {
                                smsResult.IsSuccess = false;
                                smsResult.StatusId = 5;
                                smsResult.JobNumber = sms.JobNumber;
                            }
                            this.EnQueueSMSStatus(smsResult);
                        }
                    }
                    else
                    {
                        try
                        {
                            Thread.Sleep(2000);
                        }
                        catch (ThreadInterruptedException e)
                        { }
                        catch (ThreadAbortException e)
                        { }
                    }
                }
                catch (Exception e)
                {
                    smsResult.StatusId = 5;
                    smsResult.JobNumber = "";
                    this.EnQueueSMSStatus(smsResult);
                    SharedClass.Logger.Error(e.ToString());
                }
            }
            SharedClass.Logger.Info("Exit");
        }

        private void UpdateSMSStatus()
        {
            SharedClass.Logger.Info("Started");
            SqlConnection sqlConnection = new SqlConnection(SharedClass.ConnectionString);
            SqlCommand sqlCommand = new SqlCommand("UpdateSMSStatus", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            while (!SharedClass.HasStopSignal)
            {
                try
                {
                    if (this.SMSStatusQueueCount() > 0)
                    {
                        SMSResult smsResult = this.DeQueueSMSStatus();
                        if (smsResult != null)
                        {
                            SharedClass.Logger.Info(string.Format("Updating Status. SMS: {0}, IsSuccessfullySent: {1}", smsResult.sms.Id, smsResult.IsSuccess));
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.Add(Label.ProcedureParameters.SMS_ID, SqlDbType.BigInt).Value = smsResult.sms.Id;
                            sqlCommand.Parameters.Add(Label.ProcedureParameters.STATUS_ID, SqlDbType.Int).Value = smsResult.StatusId;
                            sqlCommand.Parameters.Add(Label.ProcedureParameters.SMS_JOB_NUMBER, SqlDbType.VarChar,50).Value = smsResult.JobNumber;
                            sqlCommand.Parameters.Add(Label.ProcedureParameters.SUCCESS, SqlDbType.Bit).Direction = ParameterDirection.Output;
                            sqlCommand.Parameters.Add(Label.ProcedureParameters.MESSAGE, SqlDbType.VarChar, 1000).Direction = ParameterDirection.Output;
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            sqlCommand.ExecuteNonQuery();
                            if (!sqlCommand.IsSuccess())
                                SharedClass.Logger.Error("Error Returned From Stored Procedure. Message: " +
                                    (sqlCommand.Parameters[Label.ProcedureParameters.MESSAGE].Value.IsDbNull() ? "NULL" : sqlCommand.Parameters[Label.ProcedureParameters.MESSAGE].Value.ToString()));
                        }
                    }
                    else
                    {
                        try
                        {
                            Thread.Sleep(2000);
                        }
                        catch (ThreadInterruptedException e)
                        { }
                        catch (ThreadAbortException e)
                        { }
                    }
                }
                catch (Exception e)
                {
                    SharedClass.Logger.Error(e.ToString());
                }
            }
            SharedClass.Logger.Info("Exit");
        }
        /// <summary>
        /// Insert the sms into memory queue
        /// </summary>
        /// <param name="sms"></param>
        /// <returns></returns>
        private bool EnQueueSMS(SMS sms)
        {
            try
            {
                while (!this._smsQueueMutex.WaitOne())
                    Thread.Sleep(20);
                this._smsQueue.Enqueue(sms);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                this._smsQueueMutex.ReleaseMutex();
            }
        }
        /// <summary>
        /// DeQueues an sms object from In-Memory Queue.
        /// </summary>
        /// <returns> SMS Object</returns>
        private SMS DeQueueSMS()
        {
            SMS sms = null;
            try
            {
                while (!this._smsQueueMutex.WaitOne())
                    Thread.Sleep(20);
                if (this._smsQueue.Count > 0)
                    sms = this._smsQueue.Dequeue();
            }
            catch (Exception e)
            {

            }
            finally
            {
                this._smsQueueMutex.ReleaseMutex();
            }
            return sms;
        }

        private bool EnQueueSMSStatus(SMSResult smsResult)
        {
            try
            {
                while (!this._smsQueueMutex.WaitOne())
                    Thread.Sleep(20);
                this._smsStatusQueue.Enqueue(smsResult);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                this._smsQueueMutex.ReleaseMutex();
            }
        }

        private SMSResult DeQueueSMSStatus()
        {
            SMSResult smsResult = null;
            try
            {
                while (!this._smsQueueMutex.WaitOne())
                    Thread.Sleep(20);
                if (this._smsStatusQueue.Count > 0)
                    smsResult = this._smsStatusQueue.Dequeue();
            }
            catch (Exception e)
            {

            }
            finally
            {
                this._smsQueueMutex.ReleaseMutex();
            }
            return smsResult;
        }

        private int SMSQueueCount()
        {
            try
            {
                while (!this._smsQueueMutex.WaitOne())
                    Thread.Sleep(20);
                return this._smsQueue.Count;
            }
            catch (Exception e)
            {
                return 0;
            }
            finally
            {
                this._smsQueueMutex.ReleaseMutex();
            }
        }
        //public static bool IsUnicode(string str1)
        //{
        //    int i;
        //    try
        //    {
        //        for (i = 0; (i < str1.Length); i++)
        //        {
        //            if (Char.ConvertToUtf32(str1, i) > 255)
        //            {
        //                return true;
        //            }
        //            //if (( AscW(str1.Substring((i - 1), 1)) > 255))
        //            //{
        //            //    return true;
        //            //}

        //        }

        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }

        //}

        //public static string convertHexa(string strmsg)
        //{
        //    string strmsg1 = null;
        //    short is_language = 0;
        //    char[] chrs = strmsg.ToCharArray();
        //    for (long i = 0; i <= chrs.Length - 1; i++)
        //        strmsg1 = strmsg1 + ("0000" + Conversion.Hex(Strings.AscW(chrs[i]))).Substring(("0000" + Conversion.Hex(Strings.AscW(chrs[i]))).Length - 4, 4);
        //    return strmsg1;
        //}

       
        //private static string GetStringFromHex(string asdf)
        //{
        //    string strRetVal = "";
        //    string str1 = asdf;
        //    for (int i = 1; i <= str1.Length; i += 2)
        //    {
        //        string midString = Microsoft.VisualBasic.Strings.Mid(str1, i, 2);
        //        Int32 code = Int32.Parse(midString, System.Globalization.NumberStyles.HexNumber);
        //        //Int32 code = Char.ConvertToUtf32(str1, i);
        //        char character = Microsoft.VisualBasic.Strings.Chr(code);
        //        strRetVal = strRetVal + character;
        //    }
        //    return strRetVal;
        //}
        private void LoadConfig()
        {
            SharedClass.InitiaLizeLogger();
            SharedClass.Logger.Info(string.Format("ConnectionString: {0}, PushThreads: {1}",
                    SharedClass.ConnectionString, SharedClass.PushThreads.ToString()));
            try
            {
                SharedClass.Logger.Info(string.Format("Fetching SMSProviderCredentials. Url: {0}, AuthKey: {1}, AuthToken: {2}",
                    System.Configuration.ConfigurationManager.AppSettings["SMSProviderUrl"],
                    System.Configuration.ConfigurationManager.AppSettings["AuthKey"],
                    System.Configuration.ConfigurationManager.AppSettings["AuthToken"]));

                SharedClass.SMSProviderAuthkey = System.Configuration.ConfigurationManager.AppSettings["AuthKey"].ToString();
                SharedClass.SMSProviderSecretKey = System.Configuration.ConfigurationManager.AppSettings["AuthToken"].ToString();
                SharedClass.SMSProvider = System.Configuration.ConfigurationManager.AppSettings["SMSProviderUrl"].ToString();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        private int SMSStatusQueueCount()
        {
            try
            {
                while (!this._smsQueueMutex.WaitOne())
                    Thread.Sleep(20);
                return this._smsStatusQueue.Count;
            }
            catch (Exception e)
            {
                return 0;
            }
            finally
            {
                this._smsQueueMutex.ReleaseMutex();
            }
        }
        
    }
    internal static class ExtensionMethods
    {
        internal static bool IsDbNull(this object input)
        {
            return input.Equals(DBNull.Value);
        }
        internal static string ToPrintableString(this SMS sms, bool detailed = false)
        {
            if (detailed)
                return string.Format("Id: {0}, MobileNumber: {1}, SenderId: {2}, Message: {3}",
                    sms.Id, sms.SenderId, sms.Message);
            else
                return sms.Id.ToString();
        }
        internal static bool IsSuccess(this SqlCommand sqlCommand)
        {
            if (sqlCommand.Parameters.Contains(Label.ProcedureParameters.SUCCESS) && !sqlCommand.Parameters[Label.ProcedureParameters.SUCCESS].Value.IsDbNull())
                return Convert.ToBoolean(sqlCommand.Parameters[Label.ProcedureParameters.SUCCESS].Value.ToString());
            else
                throw new KeyNotFoundException("Either Success is not a parameter or is a DbNull");
        }
    }
}
