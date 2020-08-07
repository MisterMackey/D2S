namespace D2S.Library.Services
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Access configuration elements
    /// <para>Can be used as a singleton for best performance, for example ConfigService.Instance</para>
    /// </summary>
	[Serializable]
    public class ConfigService
    {
        #region Members

        private static volatile ConfigService _instance;
        private static readonly object SyncRoot = new object();

        #endregion Members

        #region Properties

        /// <summary>
        /// Get an instance of the class using the singleton pattern for maximum performance
        /// <para>The class can be used directly like a static class without being instantiated</para>
        /// <para>Singleton pattern - http://en.wikipedia.org/wiki/Singleton_pattern</para>
        /// <para>Implementing the Singleton Pattern - http://www.yoda.arachsys.com/csharp/singleton.html</para>
        /// <para>The singleton pattern VS static classes - http://dotnet.org.za/reyn/archive/2004/07/07/2606.aspx</para>
        /// </summary>
        public static ConfigService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigService();
                        }
                    }
                }

                return (_instance);
            }
        }

        #endregion Properties

        #region Events

        public ConfigService()
        {
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Get a configuration string from the connectionStrings section in web config and verify that it is the expected value type
        /// <para>if the value is not available or if the convertion failed it will be logged as an error</para></summary>
        /// <param name="connectionStringValueName">string</param>
        /// <returns>string</returns>
        public string GetConnectionString(string connectionStringValueName)
        {
            var result = string.Empty;

            try
            {
                result = ConfigurationManager.ConnectionStrings[connectionStringValueName].ToString();
            }
            catch
            {
                var errorMessage = string.Format("The connection string [{0}] is not a available or invalid in Web.Config", connectionStringValueName);
                LogService.Instance.Error(errorMessage, true, false);
            }

            return result;
        }

        /// <summary>
        /// Get a configuration variable from the appSettings section in web config and verify that it is the expected value type
        /// <para>if the value is not available or if the convertion failed it will be logged as an error</para></summary>
        /// <param name="appSettingsValueName">string</param>
        /// <param name="defaultValue">bool</param>
        /// <returns>bool</returns>
        public bool GetVariableAsBoolean(string appSettingsValueName, bool defaultValue = false)
        {
            var result = false;

            try
            {
                result = Convert.ToBoolean(ConfigurationManager.AppSettings[appSettingsValueName]);
            }
            catch
            {
                result = defaultValue;

                var errorMessage = string.Format("The variable [{0}] is not a available or invalid in Web.Config", appSettingsValueName);
                LogService.Instance.Error(errorMessage, true, false);
            }

            return result;
        }

        /// <summary>
        /// Get a configuration variable from the appSettings section in web config and verify that it is the expected value type
        /// <para>if the value is not available or if the convertion failed it will be logged as an error</para></summary>
        /// <param name="appSettingsValueName">string</param>
        /// <param name="defaultValue">string</param>
        /// <returns></returns>
        public string GetVariableAsString(string appSettingsValueName, string defaultValue = "")
        {
            var result = "";

            try
            {
                result = ConfigurationManager.AppSettings[appSettingsValueName];
            }
            catch
            {
                result = defaultValue;

                var errorMessage = string.Format("The variable [{0}] is not a available or invalid in Web.Config", appSettingsValueName);
                LogService.Instance.Error(errorMessage, true, false);
            }

            return result;
        }

        /// <summary>
        /// Get a configuration variable from the appSettings section in web config and verify that it is the expected value type
        /// <para>if the value is not available or if the convertion failed it will be logged as an error</para></summary>
        /// <param name="appSettingsValueName">string</param>
        /// <param name="defaultValue">int</param>
        /// <returns>int</returns>
        public int GetVariableAsInteger(string appSettingsValueName, int defaultValue = 0)
        {
            var result = 0;

            try
            {
                result = Convert.ToInt32(ConfigurationManager.AppSettings[appSettingsValueName]);
            }
            catch
            {
                result = defaultValue;

                var errorMessage = string.Format("The variable [{0}] is not a available or invalid in Web.Config", appSettingsValueName);
                LogService.Instance.Error(errorMessage, true, false);
            }

            return result;
        }

        /// <summary>
        /// Get a configuration variable from the appSettings section in web config and verify that it is the expected value type
        /// <para>if the value is not available or if the convertion failed it will be logged as an error</para></summary>
        /// <param name="appSettingsValueName">string</param>
        /// <param name="defaultValue">int</param>
        /// <returns>double</returns>
        public double GetVariableAsDouble(string appSettingsValueName, double defaultValue = 0)
        {
            var result = 0d;

            try
            {
                result = Convert.ToDouble(ConfigurationManager.AppSettings[appSettingsValueName]);
            }
            catch
            {
                result = defaultValue;

                var errorMessage = string.Format("The variable [{0}] is not a available or invalid in Web.Config", appSettingsValueName);
                LogService.Instance.Error(errorMessage, true, false);
            }

            return result;
        }

        #endregion Methods
    }
}