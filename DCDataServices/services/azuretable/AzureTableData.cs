using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCDataServices.services.azuretable
{
    public class AzureTableData: TableEntity
    {
        ////public string PartitionKey { get; set; } // is uuid
        ////public string RowKey { get; set; } // unix timestamp.to_s + (100*i).to_s
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("PartitionKey = {0}, ", PartitionKey);
            sb.AppendFormat("RowKey = {0}, ", RowKey);
            sb.AppendFormat("Name = {0}, ", Name);
            sb.AppendFormat("Value = {0}, ", Value);
            return sb.ToString();
        }
    }
}
