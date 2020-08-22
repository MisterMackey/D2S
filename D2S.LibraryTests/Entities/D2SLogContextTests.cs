using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Entity.Core;

namespace D2S.Library.Entities.Tests
{
    [TestClass()]
    public class D2SLogContextTests
    {
        [TestMethod()]
        public void D2SLogContextTest()
        {
            var context = new D2SLogContext();
            Assert.IsNotNull(context);
        }

        [TestMethod()]
        public void D2SLogContextDbSetTest()
        {
            var context = new D2SLogContext();
            var a = context.RunLogEntries;
            var b = context.taskLogEntries;
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
        }

        [TestMethod()]
        public void DbMakeChangesTest()
        {
            var context = new D2SLogContext(DeployDatabaseIfNotExist: true);
            var a = context.RunLogEntries;
            var b = context.taskLogEntries;

            RunLogEntry entry = new RunLogEntry()
            {
                Source = "Test",
                Target = "Test",
                UserName = "Test",
                Status = "Running",
                StartTime = DateTime.Now,
                MachineName = "Test",
                Tasks = new List<TaskLogEntry>()
                {
                    new TaskLogEntry()
                    {
                        TaskName = "Test",
                        Target = "Test",
                        Status = "Running",
                        StartTime = DateTime.Now,

                    }
                }
            };

            a.Add(entry);
            context.SaveChanges();
        }

        [TestMethod()]
        public void UndesiredDatabaseMigrationPreventionTest()
        {
            //drop the DB first, or at least make sure its gone
            var c = new D2SLogContext(true);
            c.Database.Delete();
            c.SaveChanges();
            c.Dispose();

            var context = new D2SLogContext(false);
            Assert.ThrowsException<EntityException>(() => addTask(context));
        }
        private void addTask(D2SLogContext c)
        {
            c.RunLogEntries.Add(new RunLogEntry());
            c.SaveChanges();
        }
    }
}