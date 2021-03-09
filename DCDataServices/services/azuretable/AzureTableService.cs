using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UtilsLibrary;

namespace DCDataServices.services.azuretable
{
    public class AzureTableService : IDCDataService
    {
        #region vars
        private readonly AzureHelperPLS helperPLS = new AzureHelperPLS();
        private readonly string DBDirectory;
        StatusServicePipeClient sspc = null;
        #endregion //vars


        #region constructor
        public AzureTableService(string db_directory, StatusServicePipeClient sspc_loc)
        {
            DBDirectory = db_directory;
            sspc = sspc_loc;
            Task load_conn_task = helperPLS.LoadConnectionInfo();
            load_conn_task.Wait();
            if (helperPLS.IsConnectionInfoLoaded())
            {
                sspc.InternetConnectionActive = true;
            }
            else
            {
                sspc.InternetConnectionActive = false;
            }
        }
        #endregion //constructor

        #region util_funcs

        // private void InitializeConnectionParameters()
        // {

        // }
        #endregion util_funcs

        #region interface_funcs
        public void PushData(string message)
        {
            TransferDataList tdl = System.Text.Json.JsonSerializer.Deserialize<TransferDataList>(message);
            //TransferData recv_data = System.Text.Json.JsonSerializer.Deserialize<TransferData>(message);
            List<AzureTableData> to_upload = new List<AzureTableData>();
            uint unix_ts_i = (uint)((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
            string unix_ts_str = unix_ts_i.ToString();
            int postfix = 0;
            foreach (var tmp_transfer_data in tdl.Items)
            {
                foreach (var tmp_resp_data in tmp_transfer_data.Data)
                {
                    AzureTableData tmp_atd = new AzureTableData
                    {
                        PartitionKey = tmp_transfer_data.NodeName, //+ ":" + recv_data.UUID,
                        RowKey = unix_ts_str + postfix.ToString("D3"),
                        Name = tmp_resp_data.Name,
                        Value = tmp_resp_data.Value
                    };
                    postfix += 1;
                    to_upload.Add(tmp_atd);
                }
            }
            int stored_elem_count = AzureDataStore.GetNumberOfElements();
            bool succ_upload = true;
            int num_items = 0; 
            if ( (stored_elem_count > 0) && (succ_upload) )
            {
                if (stored_elem_count > 3)
                {
                    num_items = 3;
                }
                else
                {
                    num_items = stored_elem_count;
                }
                List<NodeDataDB> stored_data = AzureDataStore.GetElements(num_items);
                if (stored_data.Count > 0)
                {
                    List<AzureTableData> to_upload_stored_data = new List<AzureTableData>();
                    foreach (var st_data in stored_data)
                    {
                        List<AzureTableData> tl_atd = System.Text.Json.JsonSerializer.Deserialize<List<AzureTableData>>(st_data.NDJson);
                        to_upload_stored_data.InsertRange(to_upload_stored_data.Count, tl_atd);
                    }
                    var task_l = helperPLS.PushData(to_upload_stored_data);
                    task_l.Wait();
                    succ_upload = task_l.Result;
                    Console.WriteLine($"Upload of {to_upload_stored_data.Count} saved elements, status = {succ_upload}");
                    if (succ_upload)
                    {
                        stored_elem_count -= stored_data.Count;
                        int del_count = AzureDataStore.DeleteElements(stored_data[stored_data.Count - 1]);
                        Console.WriteLine($"deleted {del_count} elements from data base");
                    }
                }
            }

            if ( (succ_upload) && (stored_elem_count <= 0) )
            {
                var task = helperPLS.PushData(to_upload);
                task.Wait();
                succ_upload = task.Result;
                sspc.InternetConnectionActive = succ_upload;
                Console.WriteLine($"Upload of {to_upload.Count} elements, status = {succ_upload}");
                if (!succ_upload)
                {
                    NodeDataDB tmp_nddb = new NodeDataDB()
                    {
                        SampleTime = unix_ts_i,
                        NDJson = System.Text.Json.JsonSerializer.Serialize<List<AzureTableData>>(to_upload)
                    };
                    Console.WriteLine($"Writing to database {to_upload.Count} elements");
                    AzureDataStore.InsertElement(tmp_nddb);
                }
            }
        }
        #endregion //interface_funcs
    }
}
