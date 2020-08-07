using System;
using System.Collections.Concurrent;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Threading;
using D2S.Library.Utilities;
using D2S.Library.Services;

namespace D2S.Library.Extractors
{
    public class ExcelExtractor : Extractor<object[], int>
    {
        protected override Action<PipelineContext, IProducerConsumerCollection<object[]>, ManualResetEvent> PausableWorkItem
        {
            get
            {
                return (context, collection, pauseEvent) =>
                {
                    if (context == null)
                    {
                        var outputMessage = "PipelineContext is not initialized for this instance of ExcelExtractor";
                        LogService.Instance.Error(outputMessage);
                        throw new InvalidOperationException(outputMessage);
                    }
                    Application xlApp = new Application();
                    Workbook xlWorkbook = xlApp.Workbooks.Open(context.SourceFilePath);
                    _Worksheet xlWorksheet = xlWorkbook.Sheets[context.ExcelWorksheetName];
                    Range xlRange = xlWorksheet.UsedRange;

                    int ColumnCount = xlRange.Columns.Count;
                    int RowCount = xlRange.Rows.Count;
                    int FirstRowToRead;
                    if (context.FirstLineContainsHeaders) { FirstRowToRead = 2; } else { FirstRowToRead = 1; }
                    if (pauseEvent == null)
                    {
                        for (int i = FirstRowToRead; i <= RowCount; i++)
                        {
                            string[] newVal = new string[ColumnCount];
                            for (int j = 1; j <= ColumnCount; j++)
                            {
                                newVal[j - 1] = xlRange.Cells[i, j].Value2.ToString();
                            }
                            collection.TryAdd(newVal);
                        }
                    }
                    else
                    {
                        for (int i = FirstRowToRead; i <= RowCount; i++)
                        {
                            pauseEvent.WaitOne();
                            string[] newVal = new string[ColumnCount];
                            for (int j = 1; j <= ColumnCount; j++)
                            {
                                newVal[j - 1] = xlRange.Cells[i, j].Value2.ToString();
                            }
                            collection.TryAdd(newVal);
                        }
                    }
                    //clean up resources
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    //rule of thumb for releasing com objects:
                    //  never use two dots, all COM objects must be referenced and released individually
                    //  ex: [somthing].[something].[something] is bad

                    //release com objects to fully kill excel process from running in the background
                    Marshal.ReleaseComObject(xlRange);
                    Marshal.ReleaseComObject(xlWorksheet);

                    //close and release
                    xlWorkbook.Close();
                    Marshal.ReleaseComObject(xlWorkbook);

                    //quit and release
                    xlApp.Quit();
                    Marshal.ReleaseComObject(xlApp);
                };
            }
        }

        protected override Action<PipelineContext, IProducerConsumerCollection<object[]>, ManualResetEvent, IProgress<int>> ReportingWorkItem => ReportingWork;

        private void ReportingWork(PipelineContext context, IProducerConsumerCollection<object[]> output, ManualResetEvent pauseEvent, IProgress<int> progress)
        {
            if (context == null)
            {
                var outputMessage = "PipelineContext is not initialized for this instance of ExcelExtractor";
                LogService.Instance.Error(outputMessage);
                throw new InvalidOperationException(outputMessage);
            }

            Application xlApp = new Application();
            Workbook xlWorkbook = xlApp.Workbooks.Open(context.SourceFilePath);
            _Worksheet xlWorksheet = xlWorkbook.Sheets[context.ExcelWorksheetName];
            Range xlRange = xlWorksheet.UsedRange;
            try
            {
                int ColumnCount = xlRange.Columns.Count;
                int RowCount = xlRange.Rows.Count;
                int FirstRowToRead;
                int progressCount = 0;
                if (context.FirstLineContainsHeaders) { FirstRowToRead = 2; } else { FirstRowToRead = 1; }
                if (pauseEvent == null)
                {
                    for (int i = FirstRowToRead; i <= RowCount; i++)
                    {
                        string[] newVal = new string[ColumnCount];
                        for (int j = 1; j <= ColumnCount; j++)
                        {
                            newVal[j - 1] = xlRange.Cells[i, j].Value2.ToString();
                        }
                        output.TryAdd(newVal);
                        progressCount++;
                        if (progressCount % 1000 == 0) { progress.Report(progressCount); }
                    }
                }
                else
                {
                    for (int i = FirstRowToRead; i <= RowCount; i++)
                    {
                        pauseEvent.WaitOne();
                        string[] newVal = new string[ColumnCount];
                        for (int j = 1; j <= ColumnCount; j++)
                        {
                            newVal[j - 1] = xlRange.Cells[i, j].Value2.ToString();
                        }
                        output.TryAdd(newVal);
                        progressCount++;
                        if (progressCount % 1000 == 0) { progress.Report(progressCount); }
                    }
                }
                progress.Report(progressCount);
            }
            finally
            {
                //clean up resources
                GC.Collect();
                GC.WaitForPendingFinalizers();

                //rule of thumb for releasing com objects:
                //  never use two dots, all COM objects must be referenced and released individually
                //  ex: [somthing].[something].[something] is bad

                //release com objects to fully kill excel process from running in the background
                Marshal.ReleaseComObject(xlRange);
                Marshal.ReleaseComObject(xlWorksheet);

                //close and release
                xlWorkbook.Close();
                Marshal.ReleaseComObject(xlWorkbook);

                //quit and release
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);
            }
        }
    }
}
