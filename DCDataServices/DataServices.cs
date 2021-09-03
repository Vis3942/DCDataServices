using DCDataServices.services.api.v1;
using DCDataServices.services.azuretable;
using LogUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DCDataServices
{
    public class DataServices
    {
        #region vars
        private const string STARTUP_OPTIONS_FILENAME = "dcds_startup_options.json";
        //private StartupOption startupOption = null;
        private DataServicePipeClient dataServicePipeClient = null;
        private IDCDataService dataService = null;
        private StatusServicePipeClient sspc = null;
        #endregion // vars

        #region property

        #endregion //property

        #region construtor
        public DataServices()
        {
            LoadActiveService();
        }

        private void LoadActiveService()
        {
            sspc = new StatusServicePipeClient();
            sspc.InternetConnectionActive = false; // initial state
            sspc.StartClient();
            if (AppOptions.GetAppOptions.Service.ToLower().Equals("azuretable"))
            {
                dataService = new AzureTableService(sspc);
            }
            else if (AppOptions.GetAppOptions.Service.ToLower().Equals("apidata"))
            {
                dataService = new APIDataService();
            }
            else
            {
                DCLogger.Information($"unsupported service in startup options {AppOptions.GetAppOptions.Service}");
            }
        }

        public void StartPipeClient()
        {
            dataServicePipeClient = new DataServicePipeClient
            {
                PipeName = AppOptions.GetAppOptions.PipeConnectionName
            };
            dataServicePipeClient.receivedData += ReceivedData;

            //dataServicePipeClient.StartClient();
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                Task t_dspc = dataServicePipeClient.RunClientAsync(cts.Token);
                t_dspc.Wait();
            }
            catch (AggregateException ae)
            {
                //throw ae.Flatten();
                ae.Flatten();
                foreach (Exception e in ae.InnerExceptions)
                {
                    DCLogger.Error(e, "errors in Data service pipe client.");
                    DCLogger.Error(e.StackTrace);
                }
            }

        }
        #endregion //constructor

        #region pipe_data
        private void ReceivedData(string recv_str)
        {
            dataService.PushData(recv_str);
        }
        #endregion //pipe_data


    }
}
