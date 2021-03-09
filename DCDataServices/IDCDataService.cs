using System;
using System.Collections.Generic;
using System.Text;

namespace DCDataServices
{
    interface IDCDataService
    {
        public void PushData(string message);
    }
}
