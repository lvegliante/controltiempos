using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace controltiempos.Function.Entities
{
   public class InputOutputEntity : TableEntity
    {
        public int EmployeeId { get; set; }
        public DateTime InputOrOutput { get; set; }
        public int Type { get; set; }
        public bool IsConsolidated { get; set; }
    }
}
