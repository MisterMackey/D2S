namespace D2S.Library.Services
{
    using System;
    using System.Data.SqlClient;
    using System.Text;
    using System.Runtime.CompilerServices;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using System.Security.Principal;
    using System.Reflection;
    using D2S.Library.Helpers;
    using System.Data;

    /// <summary>
    /// Logging related functionality (using log4net)
    /// <para>Can be used as a singleton for best performance, for example LogService.Instance</para>
    /// </summary>
    [Serializable]
    public class LogService
    {
        #region Members

        private static volatile LogService _instance;
        private static readonly object SyncRoot = new object();

        private static ILog _logger;
        // private static log4net.Util.LogicalThreadContextProperties _loggerProperties;

        private static string _originName;

        #endregion Members

        #region Properties

        /// <summary>
        /// Get an instance of the class using the singleton pattern for maximum performance
        /// <para>The class can be used directly like a static class without being instantiated</para>
        /// <para>Singleton pattern - http://en.wikipedia.org/wiki/Singleton_pattern</para>
        /// <para>Implementing the Singleton Pattern - http://www.yoda.arachsys.com/csharp/singleton.html</para>
        /// <para>The singleton pattern VS static classes - http://dotnet.org.za/reyn/archive/2004/07/07/2606.aspx</para>
        /// </summary>
        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogService();
                        }
                    }
                }

                return (_instance);
            }
        }

        /// <summary>
        /// Expose inherited flag value
        /// </summary>
        public bool IsDebugEnabled { get { return _logger.IsDebugEnabled; } }

        /// <summary>
        /// Expose inherited flag value
        /// </summary>
        public bool IsErrorEnabled { get { return _logger.IsErrorEnabled; } }

        /// <summary>
        /// Expose inherited flag value
        /// </summary>
        public bool IsFatalEnabled { get { return _logger.IsFatalEnabled; } }

        /// <summary>
        /// Expose inherited flag value
        /// </summary>
        public bool IsInfoEnabled { get { return _logger.IsInfoEnabled; } }

        /// <summary>
        /// Expose inherited flag value
        /// </summary>
        public bool IsWarnEnabled { get { return _logger.IsWarnEnabled; } }

        public string LineItemPrefix { get; private set; }

        #endregion Properties

        #region Events

        /// <summary>
        /// Set the configuration file info and initialize the logger
        /// </summary>
        public LogService()
        {
            _originName = Assembly.GetExecutingAssembly().GetName().Name;
            if (string.IsNullOrEmpty(_originName)) _originName = "Unknown";

            // Allow getting the DeclaringType in case this is used as a regular instance (not as a singleton)
            _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            XmlConfigurator.Configure();
        }

        #endregion Events

        #region Methods

        #region Fatal

        /// <summary>
        /// Log fatal information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="addLoggedFrom"></param>
        /// <param name="addIdentityInfo"></param>
        /// <param name="isHtml"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Fatal(string message = "", bool addLoggedFrom = true, bool addIdentityInfo = true, bool isHtml = false,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsFatalEnabled)
            {
                var sb = new StringBuilder();
                sb.Append(message);

                if (addIdentityInfo)
                    sb.Append(GetIdentityInfoFormatted(isHtml));

                if (addLoggedFrom)
                    sb.Append(GetLoggedFromFormatted(GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber), isHtml));

                _logger.Fatal(sb.ToString());
            }
        }

        /// <summary>
        /// Log fatal information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Fatal(Exception ex, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsFatalEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Fatal(FormatErrorMessage(ex, _originName, callerDetails, moreDetails));
            }
        }

        /// <summary>
        /// Log fatal information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="sqlEx"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Fatal(SqlException sqlEx, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsFatalEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Fatal(FormatErrorMessage(sqlEx, _originName, callerDetails, moreDetails));
            }
        }

        #endregion Fatal

        #region Error

        /// <summary>
        /// Log an error
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="addLoggedFrom"></param>
        /// <param name="addIdentityInfo"></param>
        /// <param name="isHtml"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Error(string message = "", bool addLoggedFrom = true, bool addIdentityInfo = true, bool isHtml = false,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsErrorEnabled)
            {
                var sb = new StringBuilder();
                sb.Append(message);

                if (addIdentityInfo)
                    sb.Append(GetIdentityInfoFormatted(isHtml));

                if (addLoggedFrom)
                    sb.Append(GetLoggedFromFormatted(GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber), isHtml));

                _logger.Error(sb.ToString());
            }
        }

        /// <summary>
        /// Log an error
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Error(Exception ex, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsErrorEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Error(FormatErrorMessage(ex, _originName, callerDetails, moreDetails));
            }
        }

        /// <summary>
        /// Log an error
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="sqlEx"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Error(SqlException sqlEx, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsErrorEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Error(FormatErrorMessage(sqlEx, _originName, callerDetails, moreDetails));
            }
        }

        #endregion Error

        #region Warn

        /// <summary>
        /// Log a warning
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="addLoggedFrom"></param>
        /// <param name="addIdentityInfo"></param>
        /// <param name="isHtml"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Warn(string message = "", bool addLoggedFrom = true, bool addIdentityInfo = true, bool isHtml = false,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsWarnEnabled)
            {
                var sb = new StringBuilder();
                sb.Append(message);

                if (addIdentityInfo)
                    sb.Append(GetIdentityInfoFormatted(isHtml));

                if (addLoggedFrom)
                    sb.Append(GetLoggedFromFormatted(GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber), isHtml));

                _logger.Warn(sb.ToString());
            }
        }

        /// <summary>
        /// Log a warning
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Warn(Exception ex, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsWarnEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Warn(FormatErrorMessage(ex, _originName, callerDetails, moreDetails));
            }
        }

        /// <summary>
        /// Log a warning
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="sqlEx"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Warn(SqlException sqlEx, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsWarnEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Warn(FormatErrorMessage(sqlEx, _originName, callerDetails, moreDetails));
            }
        }

        #endregion Warn

        #region Info

        /// <summary>
        /// Log information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="addLoggedFrom"></param>
        /// <param name="addIdentityInfo"></param>
        /// <param name="isHtml"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Info(string message = "", bool addLoggedFrom = true, bool addIdentityInfo = true, bool isHtml = false,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsInfoEnabled)
            {
                var sb = new StringBuilder();
                sb.Append(message);

                if (addIdentityInfo)
                    sb.Append(GetIdentityInfoFormatted(isHtml));

                if (addLoggedFrom)
                    sb.Append(GetLoggedFromFormatted(GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber), isHtml));

                _logger.Info(sb.ToString());
            }
        }

        /// <summary>
        /// Log information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Info(Exception ex, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsInfoEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Info(FormatErrorMessage(ex, _originName, callerDetails, moreDetails));
            }
        }

        /// <summary>
        /// Log information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="sqlEx"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Info(SqlException sqlEx, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsInfoEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Info(FormatErrorMessage(sqlEx, _originName, callerDetails, moreDetails));
            }
        }

        #endregion Info

        #region Debug

        /// <summary>
        /// Log debug information 
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="addLoggedFrom"></param>
        /// <param name="addIdentityInfo"></param>
        /// <param name="isHtml"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Debug(string message = "", bool addLoggedFrom = true, bool addIdentityInfo = true, bool isHtml = false,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsDebugEnabled)
            {
                var sb = new StringBuilder();
                sb.Append(message);

                if (addIdentityInfo)
                    sb.Append(GetIdentityInfoFormatted(isHtml));

                if (addLoggedFrom)
                    sb.Append(GetLoggedFromFormatted(GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber), isHtml));

                _logger.Debug(sb.ToString());
            }
        }

        /// <summary>
        /// Log debug information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Debug(Exception ex, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsDebugEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Debug(FormatErrorMessage(ex, _originName, callerDetails, moreDetails));
            }
        }

        /// <summary>
        /// Log debug information
        /// (FATAL, ERROR, WARN, INFO, DEBUG)
        /// </summary>
        /// <param name="sqlEx"></param>
        /// <param name="moreDetails"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public void Debug(SqlException sqlEx, string moreDetails = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (_logger.IsDebugEnabled)
            {
                var callerDetails = GetCallerDetails(callerMemberName, callerFilePath, callerLineNumber);

                // Note: limit the URL length as malicious attacks can add large query strings etc.
                _logger.Debug(FormatErrorMessage(sqlEx, _originName, callerDetails, moreDetails));
            }
        }

        #endregion Debug

        #region Error formatting

        /// <summary>
        /// Get caller details line for logging
        /// </summary>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        /// <returns></returns>
        public string GetCallerDetails(string callerMemberName = "", string callerFilePath = "", int callerLineNumber = 0)
        {
            return string.Format("{0}() at {1} line {2}", callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// Get current user info line for logging
        /// </summary>
        /// <returns></returns>
        public string GetCurrentIdentityInfo()
        {
            var currentUserName = WindowsIdentity.GetCurrent().Name;
            currentUserName = string.IsNullOrEmpty(currentUserName) ? "Anonymous" : $"{currentUserName}";

            return currentUserName;
        }

        public string GetIdentityInfoFormatted(bool isHtml = false)
        {
            var lineItemPrefix = isHtml ? "" : LineItemPrefix;
            var lineBreak = isHtml ? StringAndText.LineBreakHtml : StringAndText.LineBreakSystem;
            var identityInfo = GetCurrentIdentityInfo();

            return string.Format("{0}{1} User Info: {2}", lineBreak, lineItemPrefix, identityInfo);
        }

        public string GetLoggedFromFormatted(string callerDetails, bool isHtml = false)
        {
            var lineItemPrefix = isHtml ? "" : LineItemPrefix;
            var lineBreak = isHtml ? StringAndText.LineBreakHtml : StringAndText.LineBreakSystem;

            return string.Format("{0}{1} Logged From: {2}", lineBreak, lineItemPrefix, callerDetails);
        }

        /// <summary>
        /// Format an error message
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="errorOrigin"></param>
        /// <param name="callerDetails"></param>
        /// <param name="moreDetails"></param>
        /// <param name="isHtml"></param>
        /// <returns></returns>
        public string FormatErrorMessage(Exception ex, string errorOrigin = null, string callerDetails = null, string moreDetails = null, bool isHtml = false)
        {
            var lineItemPrefix = isHtml ? "" : LineItemPrefix;
            var lineBreak = isHtml ? StringAndText.LineBreakHtml : StringAndText.LineBreakSystem;
            // The use of carriage return chharacters for lineBreak is not a security issue here

            var sb = new StringBuilder();

            if (ex != null)
            {
                if (!string.IsNullOrEmpty(ex.Message)) // No lineBreak for first line
                    sb.Append(string.Format("Exception (0x{0}) - {1}", ex.HResult.ToString("X"), ex.Message));

                if (!string.IsNullOrEmpty(errorOrigin))
                    sb.Append(string.Format("{0}{1} Error Origin: {2}", lineBreak, lineItemPrefix, errorOrigin));

                var identityInfo = GetCurrentIdentityInfo();
                if (!string.IsNullOrEmpty(identityInfo))
                    sb.Append(string.Format("{0}{1} User Info: {2}", lineBreak, lineItemPrefix, identityInfo));

                if (!string.IsNullOrEmpty(callerDetails))
                    sb.Append(string.Format("{0}{1} Logged From: {2}", lineBreak, lineItemPrefix, callerDetails));

                if (!string.IsNullOrEmpty(moreDetails))
                    sb.Append(string.Format("{0}{1} More Details: {2}", lineBreak, lineItemPrefix, moreDetails));

                if (!string.IsNullOrEmpty(ex.StackTrace))
                    sb.Append(string.Format("{0}{1} Stack Trace: {2}", lineBreak, lineItemPrefix, ex.StackTrace));

                if (ex.InnerException != null)
                    sb.Append(string.Format("{0}{1} Inner Exception: {2}", lineBreak, lineItemPrefix, ex.InnerException));

                if (ex.Data.Count > 0)
                {
                    sb.Append(string.Format("{0}{1} Data: ", lineBreak, lineItemPrefix));
                    foreach (var data in ex.Data)
                    {
                        if (data != null)
                        {
                            if (data.ToString().Length == 1)
                                sb.Append(string.Format("{0}", data));
                            else sb.Append(string.Format("{0}{1}", lineBreak, data));
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Format an error message
        /// </summary>
        /// <param name="sqlEx"></param>
        /// <param name="errorOrigin"></param>
        /// <param name="callerDetails"></param>
        /// <param name="moreDetails"></param>
        /// <param name="isHtml"></param>
        /// <returns></returns>
        public string FormatErrorMessage(SqlException sqlEx, string errorOrigin = null, string callerDetails = null, string moreDetails = null, bool isHtml = false)
        {
            var lineItemPrefix = isHtml ? "" : LineItemPrefix;
            var lineBreak = isHtml ? StringAndText.LineBreakHtml : StringAndText.LineBreakSystem;
            // The use of carriage return chharacters for lineBreak is not a security issue here

            var sb = new StringBuilder();

            if (sqlEx != null)
            {
                if (!string.IsNullOrEmpty(sqlEx.Message)) // No lineBreak for first line
                    sb.Append(string.Format("SqlException (0x{0} #{1}) - {2}", sqlEx.HResult.ToString("X"), sqlEx.Number, sqlEx.Message));

                if (!string.IsNullOrEmpty(errorOrigin))
                    sb.Append(string.Format("{0}{1} Error Origin: {2}", lineBreak, lineItemPrefix, errorOrigin));

                var identityInfo = GetCurrentIdentityInfo();
                if (!string.IsNullOrEmpty(identityInfo))
                    sb.Append(string.Format("{0}{1} User Info: {2}", lineBreak, lineItemPrefix, identityInfo));

                if (!string.IsNullOrEmpty(callerDetails))
                    sb.Append(string.Format("{0}{1} Logged From: {2}", lineBreak, lineItemPrefix, callerDetails));

                foreach (SqlError sqlErr in sqlEx.Errors)
                {
                    if (!string.IsNullOrEmpty(sqlErr.Procedure))
                        sb.Append(string.Format("{0}{1} Procedure: {2}", lineBreak, lineItemPrefix, sqlErr.Procedure));

                    if (!string.IsNullOrEmpty(sqlErr.Server))
                        sb.Append(string.Format("{0}{1} Server: {2}", lineBreak, lineItemPrefix, sqlErr.Server));

                    if (!string.IsNullOrEmpty(sqlErr.Source))
                        sb.Append(string.Format("{0}{1} Source: {2}", lineBreak, lineItemPrefix, sqlErr.Source));

                    sb.Append(string.Format("{0}{1} State: {2}", lineBreak, lineItemPrefix, sqlErr.State.ToString()));
                    sb.Append(string.Format("{0}{1} Severity: {2}", lineBreak, lineItemPrefix, sqlErr.Class.ToString()));
                    sb.Append(string.Format("{0}{1} Line Number: {2}", lineBreak, lineItemPrefix, sqlErr.LineNumber.ToString()));
                }
            }

            return sb.ToString();
        }

        #endregion Error formatting

        #endregion Methods
    }
}