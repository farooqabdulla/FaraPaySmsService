using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Configuration;

namespace FaraPaySMSService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class SharedClass
    {
        #region PRIVATE VARIABLES
        private static ILog _logger = null;
        private static ILog _traceLogger = null;
        private static bool _hasStopSignal = true;
        private static bool _isServiceCleaned = true;
        private static string _connectionString = null;
        private static byte _pushThreads = 1;
        private static string _smsProviderAuthKey = string.Empty;
        private static string _smsProviderSecretKey = string.Empty;
        private static string _smsProvider = string.Empty;
        #endregion
        #region READONLY VARIABLES
        internal static readonly byte MAX_FAILED_ATTEMPTS = GetApplicationKey("MaxFailedAttempts") == null ? Convert.ToByte(0) : Convert.ToByte(GetApplicationKey("MaxFailedAttempts"));
        #endregion
        #region PRIVATE METHODS
        /// <summary>
        /// Gets the application key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private static string GetApplicationKey(string key)
        {
            return System.Configuration.ConfigurationManager.AppSettings[key];
        }
        #endregion
        #region INTERNAL METHODS
        /// <summary>
        /// Initializes the logger.
        /// </summary>
        internal static void InitiaLizeLogger()
        {
            GlobalContext.Properties["LogName"] = DateTime.Now.ToString("yyyyMMdd");
            log4net.Config.XmlConfigurator.Configure();
            _logger = LogManager.GetLogger("Logger");
            _traceLogger = LogManager.GetLogger("TraceLogger");
        }
        #endregion
        #region PROPERTIES
        internal static ILog Logger { get { return _logger; } }
        internal static ILog TraceLogger { get { return _traceLogger; } }
        internal static bool IsServiceCleaned { get { return _isServiceCleaned; } set { _isServiceCleaned = value; } }
        internal static bool HasStopSignal { get { return _hasStopSignal; } set { _hasStopSignal = value; } }
        internal static string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                    _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                return _connectionString;
            }
        }
        internal static byte PushThreads
        {
            get
            {
                if (GetApplicationKey("PushThreads") != null)
                    byte.TryParse(GetApplicationKey("PushThreads"), out _pushThreads);
                return _pushThreads;
            }
        }
        internal static string SMSProviderAuthkey { get { return _smsProviderAuthKey; } set { _smsProviderAuthKey = value; } }
        internal static string SMSProviderSecretKey { get { return _smsProviderSecretKey; } set { _smsProviderSecretKey = value; } }
        internal static string SMSProvider { get { return _smsProvider; } set { _smsProvider = value; } }
        
        #endregion
    }
}
