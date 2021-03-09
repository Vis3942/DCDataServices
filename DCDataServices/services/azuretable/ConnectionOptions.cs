using System;
using System.Collections.Generic;
using System.Text;

namespace DCDataServices.services.azuretable
{
    public class ConnectionOptions
    {
        public string Url { get; set; }
    }

    public class AccessInfo
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
    }
}
