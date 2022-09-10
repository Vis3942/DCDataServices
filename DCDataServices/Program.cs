using System;
using LogUtils;

namespace DCDataServices
{
    class Program
    {
        private static string AppConfigDirectory = "/home/debian/config/dataservices"; 
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                AppOptions.ParameterDirectory = args[0];
            }
            else
            {
                AppOptions.ParameterDirectory = AppConfigDirectory;
            }
            if (AppOptions.GetAppOptions != null )
            {
                DCLogger.InitLogger(
                    log_to: AppOptions.GetAppOptions.LogTo,
                    log_level: AppOptions.GetAppOptions.LogLevel,
                    max_file_count: AppOptions.GetAppOptions.MaxCountFiles,
                    max_file_size_mb: AppOptions.GetAppOptions.MaxLogFileSizeMB,
                    log_file_name: AppOptions.GetAppOptions.LogFileName
                );
                DCLogger.Information("Starting Data Services.");
                try
                {
                    DataServices ds = new DataServices();
                    ds.StartPipeClient();
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        DCLogger.Error(e, $"Caught in Main: {e.GetType().ToString()}");
                    }
                    DCLogger.Information("Exited DCDataServices");
                }
            }
            else
            {
                // load default options and write error info to file and exit
                DCLogger.InitLogger(
                    log_to: "file",
                    log_level: "information",
                    max_file_count: 10,
                    max_file_size_mb: 5,
                    log_file_name: "ds_app_log.txt"
                );
                DCLogger.Error($"Failed to load options from {AppConfigDirectory}");
            }
            //try
            //{
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine($"Exiting app : error {e.Message}");
            //    Console.WriteLine(e.StackTrace);
            //}
            //Console.WriteLine("press any key to exit");
            //Console.ReadLine();

        }
    }
}
