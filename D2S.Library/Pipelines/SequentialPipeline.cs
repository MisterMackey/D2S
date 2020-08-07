using D2S.Library.Extractors;
using D2S.Library.Transformers;
using D2S.Library.Loaders;
using D2S.Library.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace D2S.Library.Pipelines
{
    public abstract class SequentialPipeline : Pipeline
    {
        #region protectedFields
        /// <summary>
        /// Default delay in monitoring. Override if a custom delay is desired
        /// </summary>
        protected const int DefaultMonitoringDelayInMilliSeconds = 2000;
        /// <summary>
        /// A context object to use by deriving pipelines (uninitialized)
        /// </summary>
        protected PipelineContext m_Context;
        /// <summary>
        /// An extractor object to use by deriving pipelines (uninitialized)
        /// </summary>
        protected Extractor<string, int> m_Reader;
        /// <summary>
        /// an excel extractor object to use by deriving pipelines (uninitialized)
        /// </summary>
        protected Extractor<object[], int> m_ExcelReader;
        /// <summary>
        /// a stringsplitter object to use by deriving pipelines (uninitialized)
        /// </summary>
        protected StringSplitter m_StringSplitter;
        /// <summary>
        /// a recordtorowtransformer object to use by deriving pipelines (uninitialized)
        /// </summary>
        protected RecordToRowTransformer m_RowBuilder;
        /// <summary>
        /// a sqlTableLoader object to use by deriving pipelines (uninitialized)
        /// </summary>
        protected SQLTableLoader m_Loader;
        /// <summary>
        /// a collection of string buffers  to use by deriving pipelines (uninitialized)
        /// </summary>
        protected List<BoundedConcurrentQueu<string>> m_LineBuffers;
        /// <summary>
        /// a collection of object[] buffers  to use by deriving pipelines (uninitialized)
        /// </summary>
        protected List<BoundedConcurrentQueu<object[]>> m_RecordBuffers;
        /// <summary>
        /// a collection of row buffers  to use by deriving pipelines (uninitialized)
        /// </summary>
        protected List<BoundedConcurrentQueu<Row>> m_RowBuffers;
        #endregion

        #region protectedMethods
        /// <summary>
        /// This method monitors the given list of tasks for failures. If a task fails it will collect the errors in an aggregateexception and throw them out.
        /// When no failure occurs it waits the amount of milliseconds specified in the default monitoring delay
        /// </summary>
        /// <param name="tasks">the collection of tasks to monitor</param>
        /// <returns></returns>
        protected async Task MonitorTasksWhileReading(List<Task> tasks)
        {
            //while tasks are not faulted we perform the following
            while (!
                (tasks.Any(
                    task => task.IsFaulted)))
            {
                if (!
                    (tasks.First().IsCompleted))
                {
                    await Task.Delay(DefaultMonitoringDelayInMilliSeconds);
                }
                else
                {
                    return;
                }
            }
            //we reach this block if a tasks is faulted
            GatherExceptionsAndThrow(tasks);
        }
        /// <summary>
        /// used by monitortaskswhilereading method. Override only if custom error handling is desired
        /// </summary>
        /// <param name="tasks"></param>
        protected static void GatherExceptionsAndThrow(List<Task> tasks)
        {
            List<Exception> exceptions = new List<Exception>();
            foreach (Task t in tasks)
            {
                if (t.IsFaulted)
                {
                    exceptions.Add(t.Exception);
                }
            }

            throw new AggregateException("The following exceptions where accumulated during execution of the pipeline", exceptions);
        }
        /// <summary>
        /// Continues monitoring the list of tasks for failures. While no failures occur, it checks the given buffers for any remaining items. When empty it signals the 
        /// m_stringsplitter object and then it monitors the given number of tasks in the list starting at given index.
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="indexOfTask"></param>
        /// <param name="numTasks"></param>
        /// <param name="buffers"></param>
        /// <returns></returns>
        protected async Task UnwindStringSplitter(List<Task> tasks, int indexOfTask, int numTasks, List<BoundedConcurrentQueu<string>> buffers)
        {
            while (!
                (tasks.Any(
                    task => task.IsFaulted)))
            {
                //if any buffer in the list has anything in it
                if (buffers.Any(
                    buffer => buffer.Any()))
                {
                    await Task.Delay(DefaultMonitoringDelayInMilliSeconds);
                }
                else
                {
                    m_StringSplitter.SignalCompletion();
                    await Task.WhenAll(tasks.GetRange(indexOfTask, numTasks));
                    return;
                }
            }
            GatherExceptionsAndThrow(tasks);
        }
        /// <summary>
        /// Continues monitoring the list of tasks for failures. While no failures occur, it checks the given buffers for any remaining items. When empty it signals the 
        /// m_rowbuilder object and then it monitors the given number of tasks in the list starting at given index.
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="indexOfTask"></param>
        /// <param name="numTasks"></param>
        /// <param name="buffers"></param>
        /// <returns></returns>
        protected async Task UnwindRowBuilder(List<Task> tasks, int indexOfTask, int numTasks, List<BoundedConcurrentQueu<object[]>> buffers)
        {
            while (!
                (tasks.Any(
                    task => task.IsFaulted)))
            {
                //if any buffer in the list has anything in it
                if (buffers.Any(
                    buffer => buffer.Any()))
                {
                    await Task.Delay(DefaultMonitoringDelayInMilliSeconds);
                }
                else
                {
                    m_RowBuilder.SignalCompletion();
                    await Task.WhenAll(tasks.GetRange(indexOfTask, numTasks));
                    return;
                }
            }
            GatherExceptionsAndThrow(tasks);

        }
        /// <summary>
        /// Continues monitoring the list of tasks for failures. While no failures occur, it checks the given buffers for any remaining items. When empty it signals the 
        /// m_loader object and then it monitors the given number of tasks in the list starting at given index.
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="indexOfTask"></param>
        /// <param name="numTasks"></param>
        /// <param name="buffers"></param>
        /// <returns></returns>
        protected async Task UnwindSqlLoader(List<Task> tasks, int indexOfTask, int numTasks, List<BoundedConcurrentQueu<Row>> buffers)
        {
            while (!
                (tasks.Any(
                    task => task.IsFaulted)))
            {
                //if any buffer in the list has anything in it
                if (buffers.Any(
                    buffer => buffer.Any()))
                {
                    await Task.Delay(DefaultMonitoringDelayInMilliSeconds);
                }
                else
                {
                    m_Loader.SignalCompletion();
                    await Task.WhenAll(tasks.GetRange(indexOfTask, numTasks));
                    return;
                }
            }
            GatherExceptionsAndThrow(tasks);

        }


        #endregion


    }

}
