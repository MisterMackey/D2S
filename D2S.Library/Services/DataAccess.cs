namespace D2S.Library.Services
{
    using System;
    using System.Data;
    using System.Data.SqlClient;

    /// <summary>
    /// Data access related functions
    /// </summary>
    public class DataAccess
    {
        #region Members

        private static volatile DataAccess _instance;
        private static readonly object SyncRoot = new Object();

        #endregion Members

        #region Properties

        /// <summary>
        /// Get an instance of the class using the singleton pattern for maximum performance
        /// <para>The class can be used directly like a static class without being instantiated</para>
        /// <para>Singleton pattern - http://en.wikipedia.org/wiki/Singleton_pattern</para>
        /// <para>Implementing the Singleton Pattern - http://www.yoda.arachsys.com/csharp/singleton.html</para>
        /// <para>The singleton pattern VS static classes - http://dotnet.org.za/reyn/archive/2004/07/07/2606.aspx</para>
        /// </summary>
        public static DataAccess Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataAccess();
                        }
                    }
                }

                return (_instance);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Constructor
        /// </summary>
        public DataAccess()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the current latest Portfolio date based on [ExecutionDate]
        /// The lookup table [lkup].[Configuration_Snapshot_Date] used to find a 
        /// specific snapshot date by matching [File_Name_Identifier] to the file name.
        /// NOTE: [Current_Portfolio_Date] is expected as a result
        /// </summary>
        /// <param name="daily">Determines the portfolio date to be returned</param>
        /// <returns>DateTime or null if no record found</returns>
        public DateTime? PortfolioDate_Read(string fileNameSource)
        {
            DateTime? currentPortfolioDate = null;

            try
            {
                using (SqlConnection connectionSQL = new SqlConnection(ConfigVariables.Instance.ConfiguredConnection))
                {
                    connectionSQL.Open();

                    var storedProcedureName = "[dbo].[usp_Read_Portfolio_Date]";

                    using (SqlCommand commandSQL = new SqlCommand(storedProcedureName, connectionSQL))
                    {
                        commandSQL.CommandType = CommandType.StoredProcedure;

                        SqlParameter sqlParameterFileNameSource = new SqlParameter("@File_Name_Source", SqlDbType.NText);
                        sqlParameterFileNameSource.Direction = ParameterDirection.Input;
                        sqlParameterFileNameSource.Value = fileNameSource;
                        commandSQL.Parameters.Add(sqlParameterFileNameSource);

                        using (SqlDataReader reader = commandSQL.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["Current_Portfolio_Date"] != DBNull.Value)
                                {
                                    if (DateTime.TryParse(reader["Current_Portfolio_Date"].ToString(), out DateTime result))
                                        currentPortfolioDate = result;
                                }
                            }

                            reader.Close();
                        }

                        commandSQL.Dispose();
                    }

                    connectionSQL.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                LogService.Instance.Error(sqlEx);
                throw new ApplicationException("SqlException : " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                LogService.Instance.Error(ex);
                throw new ApplicationException("Exception : " + ex.Message);
            }

            return (currentPortfolioDate);
        }

        #endregion Methods
    }
}