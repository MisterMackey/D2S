using D2S.Library.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Entities
{
    public class D2SLogContext : DbContext
    {
        private bool DeployDatabaseIfNotExist;
        public D2SLogContext(bool DeployDatabaseIfNotExist =false) : base(ConnString())
        {
            this.DeployDatabaseIfNotExist = DeployDatabaseIfNotExist;
            if (!DeployDatabaseIfNotExist)
            {
                this.Configuration.ValidateOnSaveEnabled = false;
                Database.SetInitializer<D2SLogContext>(null);
            }
        }


        public DbSet<RunLogEntry> RunLogEntries { get; set; }
        public DbSet<TaskLogEntry> taskLogEntries { get; set; }

        private static string ConnString()
        {
            return ConfigVariables.Instance.LoggingDatabaseConnectionString;
        }
        /// <summary>
        /// mega ugly hack, don't call this method.
        /// </summary>
        public void FixEfProviderServicesProblem()
        {
            //The Entity Framework provider type 'System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer'
            //for the 'System.Data.SqlClient' ADO.NET provider could not be loaded. 
            //Make sure the provider assembly is available to the running application. 
            //See http://go.microsoft.com/fwlink/?LinkId=260882 for more information.

            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
        }
    }
}
