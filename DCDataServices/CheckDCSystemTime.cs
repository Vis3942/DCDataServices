using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DCDataServices
{
    public class CheckDCSystemTime
    {

        public static bool CheckSystemTime()
        {
            DateTime? dt = GetNetworkTime();
            if (dt.HasValue)
            {
                string dt_str = GetTimeString(dt.Value);
                var sts = SetSystemDateTime(dt_str);
                if (sts)
                {
                    LogUtils.DCLogger.Information($" successfully set DC system time to {dt_str}");
                }
                else
                {
                    LogUtils.DCLogger.Error($" failed to set DC system time to {dt_str}");
                }
                return true;
            }
            else
            {
                //LogUtils.DCLogger.Error("Failed to read time from NTP server.");
                return false;
            }
        }
        public static DateTime? GetNetworkTime()
        {
            // returns DateTime if successfully gets response from pool.ntp.org
            //    else returns null 
            try
            {
                const string ntpServer = "pool.ntp.org";
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

                var addresses = Dns.GetHostEntry(ntpServer).AddressList;
                var ipEndPoint = new IPEndPoint(addresses[0], 123);
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                socket.Connect(ipEndPoint);
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();

                ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
                ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

                return networkDateTime;

            }
            catch (Exception e)
            {
                LogUtils.DCLogger.Error(e, "Reading time from NTP server");
                return null;
            }
        }

        public static string GetTimeString(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static bool SetSystemDateTime(string new_dt_str)
        {
            string[] tstr_arr = new_dt_str.Split(new char[] { ' ' });
            if (tstr_arr.Length < 2)
            {
                Console.WriteLine($"invalid datetime string {new_dt_str}");
                return false;
            }
            var process2 = new Process();
            process2.EnableRaisingEvents = true; // to avoid [defunct] sh processes
            string set_csharp_dt = System.IO.Path.Combine(Environment.CurrentDirectory, "scripts", "settimecsharp.sh");
            Console.WriteLine(set_csharp_dt);
            if (!System.IO.File.Exists(set_csharp_dt))
            {
                Console.WriteLine("setup scrip file not found");
                return false;
            }
            process2.StartInfo.FileName = "/bin/bash";
            //string fmt_str = "'+%Y-%m-%d %H:%M:%S' -s ";
            string arg1 = tstr_arr[0].Trim();
            string arg2 = tstr_arr[1].Trim();
            process2.StartInfo.Arguments = $" \"{set_csharp_dt}\"  \"{arg1}\"  \"{arg2}\" ";
            process2.StartInfo.UseShellExecute = false;
            process2.StartInfo.CreateNoWindow = true;
            process2.Start();
            process2.WaitForExit(5 * 1000);
            if (process2.HasExited)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
