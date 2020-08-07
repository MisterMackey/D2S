namespace D2S.Library.Services
{
    using System;

    /// <summary>
    /// Global and configuration variables loaded from Web.Config
    /// <para>Safely Load variables and throw an error if they are unavailable or invalid</para>
	/// </summary>
	public class ConfigVariables
	{
		#region Members

        private static volatile ConfigVariables _instance;
        private static readonly object SyncRoot = new Object();

        #region Connection strings

        /// <summary>
        /// Connection string for Entity Framework
        /// </summary>
        public string LiqudityDatabase { get; set; }

        /// <summary>
        /// Connection string for direct SQL commands
        /// </summary>
        public string ConfiguredConnection { get; set; }

        #endregion Connection strings

        #region Global settings

        /// <summary>
        /// Field length as NVARCHAR when suggested data types are not used (default is 500)
        /// </summary>
        public int Default_Field_Length { get; set; }

        /// <summary>
        /// Number of lines to scan in order to suggest field types (default is 10000)
        /// </summary>
        public int Type_Suggestion_Sample_Lines_To_Scan { get; set; }
        

        #endregion Global settings

        #endregion Members

        #region Properties

        /// <summary>
        /// Get an instance of the class using the singleton pattern for maximum performance
        /// <para>The class can be used directly like a static class without being instantiated</para>
        /// <para>Singleton pattern - http://en.wikipedia.org/wiki/Singleton_pattern</para>
        /// <para>Implementing the Singleton Pattern - http://www.yoda.arachsys.com/csharp/singleton.html</para>
        /// <para>The singleton pattern VS static classes - http://dotnet.org.za/reyn/archive/2004/07/07/2606.aspx</para>
        /// </summary>
        public static ConfigVariables Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigVariables();
                        }
                    }
                }

                return (_instance);
            }
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Initialize config variables
        /// </summary>
        public ConfigVariables()
		{
			SafelyLoadConfigurationVariables();
        }

        #endregion Events

        #region Methods

        /// <summary>
		/// Safely Load variables from Web.Config and throw an error if one is missing
		/// </summary>
		private void SafelyLoadConfigurationVariables()
		{
            #region Connection strings

            LiqudityDatabase = ConfigService.Instance.GetConnectionString("LiqudityDatabase");
            ConfiguredConnection = ConfigService.Instance.GetConnectionString("ConfiguredConnection");

            #endregion Connection strings

            #region Global settings

            Default_Field_Length = ConfigService.Instance.GetVariableAsInteger("Default_Field_Length", 500);
            Type_Suggestion_Sample_Lines_To_Scan = ConfigService.Instance.GetVariableAsInteger("Type_Suggestion_Sample_Lines_To_Scan", 10000);

            #endregion Global settings
        }

        #endregion Methods
    }
}