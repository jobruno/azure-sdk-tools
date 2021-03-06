﻿// ----------------------------------------------------------------------------------
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Management.SqlDatabase.Database.Cmdlet
{
    using System;
    using System.Management.Automation;
    using System.Xml;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Services;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Services.Common;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Services.ImportExport;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Services.Server;
    using Microsoft.WindowsAzure.Management.Storage.Model.ResourceModel;

    /// <summary>
    /// Imports a database from blob storage into SQL Azure.
    /// </summary>
    [Cmdlet("Start", "AzureSqlDatabaseImport", ConfirmImpact = ConfirmImpact.Medium)]
    public class StartAzureSqlDatabaseImport : SqlDatabaseManagementCmdletBase
    {
        #region Parameter Set names

        /// <summary>
        /// The name of the parameter set that uses the Azure Storage Container object
        /// </summary>
        internal const string ByContainerObjectParameterSet =
            "ByContainerObject";

        /// <summary>
        /// The name of the parameter set that uses the storage container name
        /// </summary>
        internal const string ByContainerNameParameterSet =
            "ByContainerName";

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StartAzureSqlDatabaseImport"/> class.
        /// </summary>
        public StartAzureSqlDatabaseImport()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartAzureSqlDatabaseImport"/> class.
        /// </summary>
        /// <param name="channel">
        /// Channel used for communication with Azure's service management APIs.
        /// </param>
        public StartAzureSqlDatabaseImport(ISqlDatabaseManagement channel)
        {
            this.Channel = channel;
        }

        #region Parameters

        /// <summary>
        /// Gets or sets the context for connecting to the server
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            HelpMessage = "The context for connecting to the server")]
        [ValidateNotNullOrEmpty]
        public ServerDataServiceSqlAuth SqlConnectionContext { get; set; }

        /// <summary>
        /// Gets or sets the storage container object containing the blob
        /// </summary>
        [Parameter(Mandatory = true, Position = 1,
            ParameterSetName = ByContainerObjectParameterSet,
            HelpMessage = "The Azure Storage Container to place the blob in.")]
        [ValidateNotNull]
        public AzureStorageContainer StorageContainer { get; set; }

        /// <summary>
        /// Gets or sets the storage context
        /// </summary>
        [Parameter(Mandatory = true, Position = 1,
            ParameterSetName = ByContainerNameParameterSet,
            HelpMessage = "The storage connection context")]
        [ValidateNotNull]
        public AzureStorageContext StorageContext { get; set; }

        /// <summary>
        /// Gets or sets the name of the storage container to use.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2,
            ParameterSetName = ByContainerNameParameterSet,
            HelpMessage = "The name of the storage container to use")]
        [ValidateNotNullOrEmpty]
        public string StorageContainerName { get; set; }

        /// <summary>
        /// Gets or sets the name for the imported database
        /// </summary>
        [Parameter(Mandatory = true, Position = 2,
            ParameterSetName = ByContainerObjectParameterSet,
            HelpMessage = "The name for the imported database")]
        [Parameter(Mandatory = true, Position = 3,
            ParameterSetName = ByContainerNameParameterSet,
            HelpMessage = "The name for the imported database")]
        [ValidateNotNullOrEmpty]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets name of the blob to use for the import
        /// </summary>
        [Parameter(Mandatory = true, Position = 3,
            ParameterSetName = ByContainerObjectParameterSet,
            HelpMessage = "The name of the blob to use for the import")]
        [Parameter(Mandatory = true, Position = 4,
            ParameterSetName = ByContainerNameParameterSet,
            HelpMessage = "The name of the blob to use for the import")]
        [ValidateNotNullOrEmpty]
        public string BlobName { get; set; }

        /// <summary>
        /// Gets or sets the edition for the newly imported database
        /// </summary>
        [Parameter(Mandatory = false, 
            HelpMessage = "The edition for the newly imported database")]
        [ValidateNotNull]
        public DatabaseEdition Edition { get; set; }

        /// <summary>
        /// Gets or sets the maximum size for the newly imported database
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "The maximum size for the newly imported database")]
        public int DatabaseMaxSize { get; set; }

        #endregion

        /// <summary>
        /// Performs the call to import database using the server data service context channel.
        /// </summary>
        /// <param name="serverName">The name of the server to connect to.</param>
        /// <param name="input">The <see cref="ImportInput"/> object that contains 
        /// all the connection information</param>
        /// <returns>The result of the import request.  Upon success the <see cref="ImportExportRequest"/>
        /// for the request</returns>
        internal ImportExportRequest ImportSqlAzureDatabaseProcess(string serverName, ImportInput input)
        {
            ImportExportRequest result = null;

            try
            {
                XmlElement requestId = RetryCall(subscription =>
                    this.Channel.ImportDatabase(subscription, serverName, input));
                Microsoft.WindowsAzure.ServiceManagement.Operation operation = WaitForSqlDatabaseOperation();

                if (requestId != null)
                {
                    result = new ImportExportRequest();
                    result.RequestGuid = requestId.InnerText;
                }
            }
            catch (Exception ex)
            {
                SqlDatabaseExceptionHandler.WriteErrorDetails(
                    this,
                    this.SqlConnectionContext.ClientRequestId,
                    ex);
            }

            return result;
        }

        /// <summary>
        /// Process the import request
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();

                string accessKey = null;
                string blobUri = null;

                switch (this.ParameterSetName)
                {
                    case ByContainerNameParameterSet:
                        accessKey =
                            System.Convert.ToBase64String(
                                this.StorageContext.StorageAccount.Credentials.ExportKey());
                
                        blobUri = 
                            this.StorageContext.BlobEndPoint + 
                            this.StorageContainerName + "/" +
                            this.BlobName;
                        break;

                    case ByContainerObjectParameterSet:
                        accessKey =
                            System.Convert.ToBase64String(
                                this.StorageContainer.CloudBlobContainer.ServiceClient.Credentials.ExportKey());
                
                        blobUri = 
                            this.StorageContainer.Context.BlobEndPoint +
                            this.StorageContainer.Name + "/" +
                            this.BlobName;
                        break;
                }

                string fullyQualifiedServerName = 
                    this.SqlConnectionContext.ServerName + DataServiceConstants.AzureSqlDatabaseDnsSuffix;

                // Create Web Request Inputs - Blob Storage Credentials and Server Connection Info
                ImportInput importInput = new ImportInput
                {
                    BlobCredentials = new BlobStorageAccessKeyCredentials
                    {
                        StorageAccessKey = accessKey,
                        Uri = blobUri
                    },
                    ConnectionInfo = new ConnectionInfo
                    {
                        ServerName = fullyQualifiedServerName,
                        DatabaseName = this.DatabaseName,
                        UserName = this.SqlConnectionContext.SqlCredentials.UserName,
                        Password = this.SqlConnectionContext.SqlCredentials.Password
                    }
                };

                if (this.MyInvocation.BoundParameters.ContainsKey("Edition"))
                {
                    importInput.AzureEdition = this.Edition.ToString();
                }

                if (this.MyInvocation.BoundParameters.ContainsKey("DatabaseMaxSize"))
                {
                    importInput.DatabaseSizeInGB = this.DatabaseMaxSize;
                }

                ImportExportRequest request =
                    this.ImportSqlAzureDatabaseProcess(this.SqlConnectionContext.ServerName, importInput);

                if (request != null)
                {
                    request.SqlCredentials = this.SqlConnectionContext.SqlCredentials;
                    request.ServerName = this.SqlConnectionContext.ServerName;
                    this.WriteObject(request);
                }
            }
            catch (Exception ex)
            {
                SqlDatabaseExceptionHandler.WriteErrorDetails(
                    this,
                    this.SqlConnectionContext.ClientRequestId,
                    ex);
            }
        }
    }
}
