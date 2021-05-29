using System;
using System.Collections.Generic;
using System.Text;

namespace DCDataServices
{
    public class StartupOption
    {
        public string Service { get; set; }
        public string PipeConnectionName { get; set; }
        public string DBDirectory { get; set; }
        public int MaximumStoredSamples { get; set; }
        public string StatusPipeConnectionName { get; set; }
        public string AppTrueState { get; set; }
        public string AppFalseState { get; set; }
        public int PingIntervalSeconds { get; set; }
        public string LogTo { get; set; }
        public string LogLevel { get; set; }
        public float MaxLogFileSizeMB { get; set; }
        public int MaxCountFiles { get; set; }
        public string LogFileName { get; set; }
    }
}
