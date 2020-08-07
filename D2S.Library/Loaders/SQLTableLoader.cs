using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using D2S.Library.Services;

namespace D2S.Library.Loaders
{
    public class SQLTableLoader : Loader<Row, int>
    {
        public SQLTableLoader()
        {
            HasWork = true;
            LockObject = new object();
        }
        protected override Action<PipelineContext, IProducerConsumerCollection<Row>> WorkItem
        {
            get
            {
                return (context, collection) =>
                {
                    if (context == null)
                    {
                        var outputMessage = "PipelineContext is not initialized for this instance of FlatfileExtractor";
                        LogService.Instance.Error(outputMessage);
                        throw new InvalidOperationException(outputMessage);
                    }
                    using (StreamWriter Writer = new StreamWriter(context.DestinationTableName))
                    {
                        DataTable datatable = new DataTable();
                        String tablename = context.DestinationTableName;

                        foreach (string column in context.ColumnNames)
                        {
                            datatable.Columns.Add(column);
                        }

                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        while (true)
                        {
                            if (collection.Count != 0)
                            {
                                DataRow newRow = datatable.NewRow();
                                Row currentRow;
                                if (collection.TryTake(out currentRow))
                                {
                                    foreach (var item in currentRow)
                                    {
                                        newRow[item.Key] = item.Value.Item1;
                                    }
                                    datatable.Rows.Add(newRow);
                                }
                            }

                            if (sw.Elapsed > TimeSpan.FromSeconds(2)) { break; }
                        }


                        using (SqlBulkCopy Copy = new SqlBulkCopy(ConfigVariables.Instance.ConfiguredConnection, SqlBulkCopyOptions.TableLock))
                        {
                            Copy.DestinationTableName = context.DestinationTableName;
                            Copy.WriteToServer(datatable);
                            datatable.Clear();
                        }

                    }
                };
            }

        }

        protected override Action<PipelineContext, IProducerConsumerCollection<Row>, ManualResetEvent, IProgress<int>> PausableReportingWorkItem => DoPausableReportableWork;

        private void DoPausableReportableWork(PipelineContext context, IProducerConsumerCollection<Row> input, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            if (context == null)
            {
                var outputMessage = "Pipelinecontext is not initialized for this instance of SQLTableLoader";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }

            int recordsProcessed = 0;
            //context.DestinationTableName.Split('.')[1], context.DestinationTableName.Split('.')[0]
            DataTable dataTable = new DataTable(context.DestinationTableName);
            foreach (string name in context.ColumnNames)
            {
                dataTable.Columns.Add(name);                
            }
            while (HasWork)
            {
                pauseEvent.WaitOne();
                while (dataTable.Rows.Count < 10000)
                {
                    Row currentRow;
                    if (input.TryTake(out currentRow))
                    {
                        DataRow newRow = dataTable.NewRow();
                        foreach (var field in currentRow)
                        {
                            newRow[field.Key] = field.Value.Item1;
                        }
                        //scan for dbnull
                        ReplaceDbNullStringRepresentations(context, newRow);
                        dataTable.Rows.Add(newRow);
                        recordsProcessed++;
                    }
                    else if (!HasWork)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(10); //yield slice to other tasks
                    }
                }
                //after 10 000 rows are saved up we write to db so we can clear the buffer and keep memory consumption down
                //todo: change implementation to use IDatareader
                using (SqlBulkCopy Copy = new SqlBulkCopy(ConfigVariables.Instance.ConfiguredConnection, SqlBulkCopyOptions.TableLock))
                {
                    Copy.DestinationTableName = dataTable.TableName;
                    Copy.BulkCopyTimeout = 0;
                    Copy.WriteToServer(dataTable);                    
                    progress.Report(recordsProcessed);
                    dataTable.Clear();
                }
            }
        }

        private static void ReplaceDbNullStringRepresentations(PipelineContext context, DataRow newRow)
        {
            if (context.DbNullStringValue != null)
            {
                for (int i = 0; i < newRow.ItemArray.Length; i++)
                {
                    if (newRow[i].ToString() == context.DbNullStringValue)
                    {
                        newRow[i] = DBNull.Value;
                    }
                }
            }
        }
    }
}
