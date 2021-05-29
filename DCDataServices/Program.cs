using System;
using LogUtils;

namespace DCDataServices
{
    class Program
    {
        static void Main()
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
