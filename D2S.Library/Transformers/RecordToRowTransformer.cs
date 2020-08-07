using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2S.Library.Utilities;
using D2S.Library.Services;

namespace D2S.Library.Transformers
{
    public class RecordToRowTransformer : Transformer<object[], Row, int>
    {
        private readonly string[] ColumnNames;
        private readonly bool IsSkippingError;
        /// <summary>
        /// Creates a new instance of the RecordToRowTransformer, which transforms a collection of object arrays into a collection of Row objects.
        /// </summary>
        /// <param name="columnNames">An array containing the column names of the fields that are in the input collection. Names must be in the correct order.</param>
        /// <param name="SkipErrors">A bool indicating if rows with too few or too many columns (i.e. data errors) should be skipped over (true) or break the process (false). Default is false.</param>
        public RecordToRowTransformer(string[] columnNames, bool SkipErrors = false)
        {
            HasWork = true;
            LockingObject = new object();
            ColumnNames = columnNames;
            IsSkippingError = SkipErrors;
        }

        protected override Action<IProducerConsumerCollection<object[]>, IProducerConsumerCollection<Row>, ManualResetEvent, IProgress<int>> ReportingWorkItem => DoWorkAndReport;

        public override void SignalCompletion()
        {
            lock (LockingObject) { HasWork = false; }
        }

        private void DoWorkAndReport(IProducerConsumerCollection<object[]> input, IProducerConsumerCollection<Row> output, ManualResetEvent pauseEvent, IProgress<int> progressMonitor)
        {
            RowFactory Factory = new RowFactory(ColumnNames);
            int ExpectedColumnCount = ColumnNames.Count();
            int processedCount = 0;
            while (HasWork)
            {
                pauseEvent.WaitOne();
                object[] currentInput;
                if (input.TryTake( out currentInput))
                {

                    if (ExpectedColumnCount != currentInput.Count())
                    {
                        var errorMsg = $"A row was skipped over because it had too many or too few columns, expected: {ExpectedColumnCount}, actual: {currentInput.Count()}";
                        if (IsSkippingError)
                        {
                            LogService.Instance.Warn(errorMsg);   
                        }
                        else
                        {
                            Exception ex = new Exception(errorMsg);
                            LogService.Instance.Error(ex);
                            throw ex;
                        }

                    }
                    else
                    {
                        Row newRow = Factory.CreateRow(currentInput);
                        while (!output.TryAdd(newRow)) { pauseEvent.WaitOne(); }
                        processedCount++;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
                if (processedCount % 1000 == 0)
                {
                    progressMonitor.Report(processedCount);
                }
            }
            progressMonitor.Report(processedCount);
        }
    }
}
