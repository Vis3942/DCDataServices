using LogUtils;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DCDataServices.services.azuretable
{
    public class AzureHelperPLS
    {
        #region vars
        private ConnectionOptions startOpts = null;
        private const string AZURE_CONNECTION_INFO_FILENAME = "azure_connection_info.json";
        private CloudStorageAccount storageAccount=null;
        CloudTableClient tableClient = null;
        private readonly HttpClient httpClient = new HttpClient();
        CloudTable cloudTable = null;
        AccessInfo accessInfo = null;
        private string pdErrorMessage = "";
        private bool internet_available_time_set = false;
        #endregion //vars

        #region property
        public string GetPushDataErrorMessage
        {
            get { return pdErrorMessage; }
        }
        #endregion // property

        public AzureHelperPLS()
        {
        }

        public bool IsConnectionInfoLoaded()
        {
            if (startOpts != null)
            {
                if (accessInfo != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> LoadConnectionInfo()
        {
            string conn_info_fname = System.IO.Path.Combine(AppOptions.ParameterDirectory, AZURE_CONNECTION_INFO_FILENAME);
            if (System.IO.File.Exists(conn_info_fname))
            {
                string json_str_from_file = System.IO.File.ReadAllText(conn_info_fname);
                startOpts = System.Text.Json.JsonSerializer.Deserialize<ConnectionOptions>(json_str_from_file);
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(startOpts.Url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    //debug
                    DCLogger.Debug(responseBody);
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);
                    var rootElement = jsonDoc.RootElement;
                    var connElement = rootElement.GetProperty("conn");
                    var conn_str = $"DefaultEndpointsProtocol=https;AccountName={connElement.GetProperty("ac_n").GetString()};AccountKey={connElement.GetProperty("ac_k").GetString()};EndpointSuffix=core.windows.net";
                    accessInfo = new AccessInfo
                    {
                        ConnectionString = conn_str,
                        TableName = connElement.GetProperty("tb_n").GetString()
                    };
                    return true;
                }
                catch (HttpRequestException e)
                {
                    DCLogger.Error(e, "During Http Request");
                    if (e.InnerException != null)
                    {
                        DCLogger.Error(e.InnerException, "During Http Request: Innter Exception");
                    }
                    startOpts = null;
                    accessInfo = null;
                    return false;
                }
            }
            else
            {
                DCLogger.Error($"Failed to locate connection info filename {conn_info_fname}");
                return false;
            }
        }

        private bool GetStorageAccount()
        {
            try
            {
                if (accessInfo == null)
                {
                    DCLogger.Error("No info from endpoint for computing access info.");
                    return false;
                   
                }
                storageAccount = CloudStorageAccount.Parse(accessInfo.ConnectionString);
                // Create a table client for interacting with the table service
                tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                return true;
            }
            catch (Exception ex)
            {
                storageAccount = null;
                tableClient = null;
                DCLogger.Error(ex, $"Getting storage account");
                return false;
            }
        }


        public async Task<bool> PushData(List<AzureTableData> to_push_data)
        {
            pdErrorMessage = "";
            bool i_sts = false;
            if (!internet_available_time_set)
            {
                i_sts = CheckDCSystemTime.CheckSystemTime();
                if (!i_sts)
                {
                    internet_available_time_set = false;
                    return false;
                }
                else
                {
                    internet_available_time_set = true;
                }
            }

            // assume that all the items in the list to_push_data have the same partition key
            if (startOpts == null)
            {
                Task<bool> t1 = LoadConnectionInfo();
                t1.Wait();
                if (!t1.Result)
                {
                    pdErrorMessage = "Failed to load connection info";
                    return false;
                }
            }
            if (storageAccount == null)
            {
                bool sts = GetStorageAccount();
                if (!sts)
                {
                    pdErrorMessage = "Failed to obtain storage account";
                    return false;
                }
            }

            if ( (startOpts != null) && (storageAccount != null) && (tableClient != null) )
            {
                try
                {
                    if (cloudTable == null)
                    {
                        cloudTable = tableClient.GetTableReference(accessInfo.TableName);
                        if (await cloudTable.CreateIfNotExistsAsync())
                        {
                            DCLogger.Information($"Created Table named: {accessInfo.TableName}");
                        }
                    }
                    TableBatchOperation batchOperation = new TableBatchOperation();
                    foreach (var tmp_entry in to_push_data)
                    {
                        batchOperation.InsertOrReplace(tmp_entry);
                    }
                    TableBatchResult result = await cloudTable.ExecuteBatchAsync(batchOperation);
                    if (result.Count >= batchOperation.Count)
                    {
                        return true;
                    }
                    else
                    {
                        pdErrorMessage = $"Failed at ExecuteBatchAsync. Invalid count in response uploaded {batchOperation.Count} result = {result.Count}";
                        return false; 
                    }
                }
                catch (Exception ex)
                {
                    DCLogger.Error(ex, $"Table batch operation upload failed. {accessInfo.TableName}");
                    //throw;
                    pdErrorMessage = ex.Message;
                    return false;
                }

            }
            else
            {
                DCLogger.Error($"Table upload failed {accessInfo.TableName}");
                DCLogger.Error($"Table upload failed startopts = {startOpts == null}, account = {storageAccount == null}, tableclient = {tableClient == null}");
                pdErrorMessage = "one or more of startOpts, storageAccount, tableClient was null";
                return false;
            }
        }

    }
}
