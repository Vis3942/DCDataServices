using System;

namespace DCDataServices
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting Data Services.");
            try
            {
                DataServices ds = new DataServices();
                ds.StartPipeClient();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    Console.WriteLine($"Main: {e.GetType().Name}: {e.Message}");
                }
                Console.WriteLine("Exited DCDataServices");
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
