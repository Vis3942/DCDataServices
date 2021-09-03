using LogUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UtilsLibrary;

namespace DCDataServices.services.azuretable
{
    public class AzureTableService : IDCDataService
    {
        #region vars
        private readonly AzureHelperPLS helperPLS = new AzureHelperPLS();
        //private readonly string DBDirectory;
        StatusServicePipeClient sspc = null;
        private readonly string UPLOAD_FAIL_COUNT_FILENAME = "UploadFailCount.txt";
        private readonly int count_update_past_datasets; // max number of sets to push if in data store
        #endregion //vars


        #region constructor
        public AzureTableService(StatusServicePipeClient sspc_loc)
        {
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
            count_update_past_datasets = AppOptions.GetAppOptions.DBPastDataUpdateCount;
        }
        #endregion //constructor

        #region util_funcs

        // private void InitializeConnectionParameters()
        // {

        // }
        private void CheckUploadStatus(bool curr_upload_status)
        {
            if (curr_upload_status)
            {
                System.IO.File.WriteAllText(UPLOAD_FAIL_COUNT_FILENAME, "0");
            }
            else
            {
                int lost_upload_count;
                bool sts;
                var lost_upload_count_str = System.IO.File.ReadAllText(UPLOAD_FAIL_COUNT_FILENAME);
                sts = int.TryParse(lost_upload_count_str, out lost_upload_count);
                if (sts)
                {
                    lost_upload_count++;
                    System.IO.File.WriteAllText(UPLOAD_FAIL_COUNT_FILENAME, lost_upload_count.ToString());
                    //if (lost_upload_count > 6)
                    //{
                    //    // need to inform azure endpoint as we have tried 3 reboots already.
                    //}
                    //else 
                    if (lost_upload_count % 2 == 0)
                    {
                        // Exit app; and service will restart it
                        if (lost_upload_count > 3)
                        {
                            if (helperPLS.GetPushDataErrorMessage.Contains("Make sure the value of Authorization header is formed correctly"))
                            {
                                // execute script to reboot the DC
                                reboot_dc(); // possible timing error on upload to Azure
                            }
                            // and error is "Make sure value of Authorization header is formed correctly"
                        }
                        System.Environment.Exit(-1);
                    }
                }
                else
                {
                    System.IO.File.WriteAllText(UPLOAD_FAIL_COUNT_FILENAME, "1");
                }
            }
        }

        private void reboot_dc()
        {
            // write 1 to /home/debian/dc_restart/schedule_dc_reboot.txt
            string wr_fname = AppOptions.GetAppOptions.RestartFileName;
            try
            {
                DCLogger.Information("Required Reboot DC.");
                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter(path: wr_fname);
                //Write a line of text
                sw.WriteLine("1");
                //Write a second line of text
                //Close the file
                sw.Close();
            }
            catch (Exception e)
            {
                DCLogger.Error(e, "writing restart flag to file");
            }
        }
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
                if (stored_elem_count > count_update_past_datasets)
                {
                    num_items = count_update_past_datasets;
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
                    sspc.InternetConnectionActive = succ_upload;
                    DCLogger.Information($"Upload of {to_upload_stored_data.Count} saved elements, status = {succ_upload}");
                    if (succ_upload)
                    {
                        stored_elem_count -= stored_data.Count;
                        int del_count = AzureDataStore.DeleteElements(stored_data[stored_data.Count - 1]);
                        DCLogger.Information($"deleted {del_count} elements from data base");
                    }
                }
            }

            if ( (succ_upload) && (stored_elem_count <= 0) )
            {
                var task = helperPLS.PushData(to_upload);
                task.Wait();
                succ_upload = task.Result;
                sspc.InternetConnectionActive = succ_upload;
                DCLogger.Information($"Upload of {to_upload.Count} elements, status = {succ_upload}");
                DCLogger.Information($"for node {to_upload[0].PartitionKey}, rowkey = {to_upload[0].RowKey}");
                if (!succ_upload)
                {
                    NodeDataDB tmp_nddb = new NodeDataDB()
                    {
                        SampleTime = unix_ts_i,
                        NDJson = System.Text.Json.JsonSerializer.Serialize<List<AzureTableData>>(to_upload)
                    };
                    DCLogger.Information($"Writing to database {to_upload.Count} elements");
                    AzureDataStore.InsertElement(tmp_nddb);
                }
            }
            else
            {
                if (to_upload.Count > 0)
                {
                    NodeDataDB tmp_nddb_2 = new NodeDataDB()
                    {
                        SampleTime = unix_ts_i,
                        NDJson = System.Text.Json.JsonSerializer.Serialize<List<AzureTableData>>(to_upload)
                    };
                    DCLogger.Information($"Writing to database {to_upload.Count} elements");
                    AzureDataStore.InsertElement(tmp_nddb_2);
                }
            }
            CheckUploadStatus(succ_upload);
        }
        #endregion //interface_funcs
    }
}
