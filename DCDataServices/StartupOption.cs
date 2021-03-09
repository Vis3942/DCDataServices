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
    }
}
