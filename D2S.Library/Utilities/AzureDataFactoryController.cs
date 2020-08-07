using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Services;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;

namespace D2S.Library.Utilities
{
    //see https://docs.microsoft.com/nl-nl/azure/data-factory/monitor-programmatically for the references used when writing the start and monitor methods.
    /// <summary>
    /// Class which allows a consumer to start and monitor pipeline runs which exist in a specified data factory. A instantiation will be needed if different data factories are used, but multiple pipelines can be started with one 
    /// object of this type as long as they all reside in the same data factory resource.
    /// </summary>
    public class AzureDataFactoryController
    {
        #region fields
        private readonly AzureClient m_AzureClient;
        private DataFactoryManagementClient m_DataFactory;
        private readonly string m_DataFactoryName;
        private readonly string m_ResourceGroupName;
        private readonly string m_SubscriptionId;
        #endregion

        #region Constructors

        

        /// <summary>
        /// Creates a new instance of the <see cref="AzureDataFactoryController"/> Class using the specified values
        /// </summary>
        /// <param name="SubscriptionId">Name of the subscription in which the resource group is located</param>
        /// <param name="ResourceGroup">Name of the Resource group in which the data factory resides</param>
        /// <param name="DataFactoryName">name of the data factory</param>
        public AzureDataFactoryController(PipelineContext context, string ResourceGroup , string DataFactoryName)
        {
            m_AzureClient = new AzureClient(context);
            m_SubscriptionId = context.AzureSubscription;
            m_DataFactoryName = DataFactoryName;
            m_ResourceGroupName = ResourceGroup;
            Login();
        }

        #endregion

        #region publicMethods
        /// <summary>
        /// Starts the pipeline with the given name using the parameters specified.
        /// </summary>
        /// <param name="PipelineName">The name of the pipeline to execute</param>
        /// <param name="parameters">The parameters to use when executing the pipeline. These must be passed as a dictionary where the entries are the name of the parameters as the keys and the value as the values</param>
        /// <returns>A unique identifier for the started run which can be used to check the status and retrieve messages</returns>
        public async Task<string> StartPipelineAsync(string PipelineName, IDictionary<string, object> parameters)
        {
            var Run = m_DataFactory.Pipelines.CreateRunWithHttpMessagesAsync(m_ResourceGroupName, m_DataFactoryName, PipelineName, parameters:parameters);
            
            await Run;

            var Response = Run.Result.Body;
            return Response.RunId;
        }

        /// <summary>
        /// Starts the pipline with the given name, omitting the parameter option.
        /// </summary>
        /// <param name="PipelineName">The name of the pipeline to execute</param>
        /// <returns>A unique identifier for the started run which can be used to check the status and retrieve messages</returns>
        public async Task<string> StartPipelineAsync(string PipelineName)
        {
            string result = await StartPipelineAsync(PipelineName, null);
            return result;
        }

        /// <summary>
        /// Returns the status of the run associated with the runId given. Does not return the raw string given by the underlying API
        /// </summary>
        /// <param name="runId">A unique identifier for the run</param>
        /// <returns>A pipelinerunstatus enumerable indicating the status of the run</returns>
        public PipelineRunStatus CheckStatus(string runId)
        {
            var run = m_DataFactory.PipelineRuns.Get(m_ResourceGroupName, m_DataFactoryName, runId);

            string status = run.Status;
            switch (status)
            {
                case "InProgress":
                    return PipelineRunStatus.InProgress;
                case "Succeeded":
                    return PipelineRunStatus.Succeeded;
                default:
                    return PipelineRunStatus.Error;
            }
        }

        /// <summary>
        /// Retrieves the latest output from the pipeline associated with the given runId
        /// </summary>
        /// <param name="runId">A unique identifier for the run</param>
        /// <returns>A string with the latest Output (or error message in case of failure)</returns>
        public string RetrieveLatestOutputFromPipeline(string runId)
        {
            RunFilterParameters filter = new RunFilterParameters(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            List<ActivityRun> activities = m_DataFactory.ActivityRuns.QueryByPipelineRun(m_ResourceGroupName, m_DataFactoryName, runId, filter).Value.ToList();

            if (CheckStatus(runId) == PipelineRunStatus.Error)
            {
                return $"Error in pipeline, message: {activities.First().Error}"; //apparently the error object is a string? just going by the midcrosoft ref here.
            }
            else
            {
                return activities.First().Output.ToString();
            }
        }
        #endregion

        #region PrivateMethods
        private void Login()
        {
            m_DataFactory = m_AzureClient.GetDataFactoryClient(m_SubscriptionId);
        }
        #endregion

        #region Enumerables
        public enum PipelineRunStatus
        {
            InProgress,
            Succeeded,
            Error
        }
        #endregion
    }
}
