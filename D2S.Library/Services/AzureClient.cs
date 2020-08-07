using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates; // Required only if you are using an Azure AD application created with certificates

using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;

using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.DataLake.Store;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;
using D2S.Library.Utilities;

namespace D2S.Library.Services
{
    public class AzureClient
    {

        #region  variables
        private readonly string TENANT;
        private readonly string CLIENTID;
        private readonly string SecretKey;        
        private readonly System.Uri ADL_TOKEN_AUDIENCE = new System.Uri(@"https://datalake.azure.net/");

        #endregion

        #region constructor
        public AzureClient(PipelineContext context)
        {
            TENANT = context.AzureTenant;
            CLIENTID = context.AzureClientId;
            SecretKey = context.AzureSecretKey;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Retrieves a data lake client using the default credentials and data lake name/adress
        /// </summary>
        /// <returns>A datalake client which can access the default data lake storage from appsettings</returns>
        public AdlsClient GetDataLakeClient()
        {
            var adress = ConfigurationManager.AppSettings.Get("DatalakeAdress");
            return GetDataLakeClient(adress, false);
        }

        /// <summary>
        /// Retrieves a data lake client using the default credentials and the data lake name/adress specified by the caller
        /// </summary>
        /// <param name="dataLakeAdress">adress to a datalake within the ABN AMRO domain, in the form of "Name.azuredatalakestore.net" </param>
        /// <returns></returns>
        public AdlsClient GetDataLakeClient(string dataLakeAdress)
        {
            return GetDataLakeClient(dataLakeAdress, false);
        }

        /// <summary>
        /// Retrieves a data lake client using the default datalek name and using either the default credentials or prompting the user to enter a username/password to authenticate with. 
        /// </summary>
        /// <param name="promptUserLoginScreen">Boolean specifying whether or not to prompt for a login screen</param>
        /// <returns></returns>
        public AdlsClient GetDataLakeClient(bool promptUserLoginScreen)
        {
            var adress = ConfigurationManager.AppSettings.Get("DatalakeAdress");
            return GetDataLakeClient(adress, promptUserLoginScreen);
        }

        /// <summary>
        /// Retrieves a data lake client using the specified adress and either the default credentials or by prompting the user to enter a username/password to authenticate with
        /// </summary>
        /// <param name="dataLakeAdress">adress to a datalake within the ABN AMRO domain, in the form of "Name.azuredatalakestore.net" </param>
        /// <param name="promptUserLoginScreen">Boolean specifying whether or not to prompt for a login screen</param>
        /// <returns></returns>
        public AdlsClient GetDataLakeClient(string dataLakeAdress, bool promptUserLoginScreen)
        {
            AdlsClient client;
            ServiceClientCredentials adlCreds;
            if (promptUserLoginScreen)
            {
                adlCreds = GetCreds_User_Popup(TENANT, ADL_TOKEN_AUDIENCE, CLIENTID);
                client = AdlsClient.CreateClient(dataLakeAdress, adlCreds);
            }
            else
            {
                adlCreds = GetCreds_SPI_SecretKey(TENANT, ADL_TOKEN_AUDIENCE, CLIENTID, SecretKey);
                client = AdlsClient.CreateClient(dataLakeAdress, adlCreds); 
            }

            return client;
        }

        /// <summary>
        /// Retrieves a data factory client using the default credentials and the default subscription.
        /// </summary>
        /// <returns></returns>
        public DataFactoryManagementClient GetDataFactoryClient()
        {
            var subs = ConfigurationManager.AppSettings.Get("AzureSubscription");
            return GetDataFactoryClient(subs);
        }

        /// <summary>
        /// Retrieves a data factory client using the default credentials and the specified subscription.
        /// </summary>
        /// <param name="SubScriptionId">The name of the subscription</param>
        /// <returns></returns>
        public DataFactoryManagementClient GetDataFactoryClient(string SubScriptionId)
        {
            ServiceClientCredentials AdfCreds = GetAdfCredsUsingSecretKey(TENANT, CLIENTID, SecretKey);

            return new DataFactoryManagementClient(AdfCreds) { SubscriptionId = SubScriptionId};
        }

        #endregion

        #region private methods
        //see: https://azure.microsoft.com/en-us/resources/samples/data-lake-analytics-dotnet-auth-options/
        private static ServiceClientCredentials GetCreds_User_Popup(
           string tenant,
           System.Uri tokenAudience,
           string clientId,           
           PromptBehavior promptBehavior = PromptBehavior.Auto)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            var clientSettings = new ActiveDirectoryClientSettings
            {
                ClientId = clientId,
                ClientRedirectUri = new System.Uri("urn:ietf:wg:oauth:2.0:oob"),
                PromptBehavior = promptBehavior
            };

            var serviceSettings = ActiveDirectoryServiceSettings.Azure;
            serviceSettings.TokenAudience = tokenAudience;

            var creds = UserTokenProvider.LoginWithPromptAsync(
               tenant,
               clientSettings,
               serviceSettings).GetAwaiter().GetResult();            
            return creds;
        }

        //see: https://azure.microsoft.com/en-us/resources/samples/data-lake-analytics-dotnet-auth-options/
        private static ServiceClientCredentials GetCreds_SPI_SecretKey(
           string tenant,
           Uri tokenAudience,
           string clientId,
           string secretKey)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            var serviceSettings = ActiveDirectoryServiceSettings.Azure;
            serviceSettings.TokenAudience = tokenAudience;

            var creds = ApplicationTokenProvider.LoginSilentAsync(
             tenant,
             clientId,
             secretKey,
             serviceSettings).GetAwaiter().GetResult();
            return creds;
        }

        //see: https://azure.microsoft.com/en-us/resources/samples/data-lake-analytics-dotnet-auth-options/
        private ServiceClientCredentials GetAdfCredsUsingSecretKey(string tenantID, string applicationId, string authenticationKey)
        {
            var context = new AuthenticationContext("https://login.windows.net/" + tenantID);
            ClientCredential cc = new ClientCredential(applicationId, authenticationKey);
            AuthenticationResult result = context.AcquireTokenAsync("https://management.azure.com/", cc).Result;
            ServiceClientCredentials cred = new TokenCredentials(result.AccessToken);
            return cred;
        }

        #endregion
    }
}
