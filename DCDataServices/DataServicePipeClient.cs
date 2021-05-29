using LogUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DCDataServices
{
   public class DataServicePipeClient
   {
        #region vars
        private string _pipe_name = "";
        private NamedPipeClientStream clientPipe = null;
        private StreamWriter swriter = null;
        private StreamReader sreader = null;
        private CancellationTokenSource pipeClientTokenSrc = new CancellationTokenSource();
        public Action<string> receivedData = null;

        #endregion // vars

        #region property
        public string PipeName
        {
            get { return _pipe_name; }
            set { _pipe_name = value; }
        }

        public bool IsConnected
        {
            get
            {
                if (clientPipe != null)
                {
                    return clientPipe.IsConnected;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion // property

        public void StartClient()
        {
            if (clientPipe == null)
            {
                Task.Factory.StartNew(() => RunClientAsync(pipeClientTokenSrc.Token), pipeClientTokenSrc.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                //Task.Run(() => RunClientAsync(pipeClientTokenSrc.Token), pipeClientTokenSrc.Token);
            }
            else
            {
                DCLogger.Information("Client already running.");
            }
        }


        public void StopClient()
        {
            if (clientPipe != null)
            {
                pipeClientTokenSrc.Cancel();
                //if (clientPipe.IsConnected)
                //{
                    clientPipe.Close();
                //}
                clientPipe.Dispose();
                clientPipe = null;
                System.Threading.Thread.Sleep(1000);
                pipeClientTokenSrc = new CancellationTokenSource();
            }
        }

        public async Task RunClientAsync(CancellationToken cts)
        {
            while (!cts.IsCancellationRequested)
            {
                DCLogger.Information("initializing clientPipe");
                clientPipe = new NamedPipeClientStream(".", _pipe_name, PipeDirection.InOut, PipeOptions.Asynchronous);
                DCLogger.Information("attempting to connect.");
                await clientPipe.ConnectAsync();
                DCLogger.Information("successful connection");

                swriter = new StreamWriter(clientPipe)
                {
                    AutoFlush = true
                };
                sreader = new StreamReader(clientPipe);

                while (!cts.IsCancellationRequested)
                {
                    string line = await sreader.ReadLineAsync();
                    // debug
                    DCLogger.Debug(string.Format("pipe client: Rd: [{0}]", line));
                    if ((line == null) || (line.Equals("end")) || (line.Length == 0))
                    {
                        break;
                    }
                    else
                    {
                        receivedData?.Invoke(line);
                    }
                }
                sreader.Close();
                sreader.Dispose();
                sreader = null;
                swriter = null;
            }
        }

        public void WriteToPipe(string message)
        {
            if (((clientPipe != null) && (clientPipe.IsConnected)) && (swriter != null))
            {
                swriter.WriteLine(message);
            }
            else
            {
                DCLogger.Information("is it connected.");
            }
        }
    }
}
