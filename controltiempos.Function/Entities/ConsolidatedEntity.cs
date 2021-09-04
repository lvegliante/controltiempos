using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace controltiempos.Function.Entities
{
    public class ConsolidatedEntity : TableEntity
    {
        public int EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public int MinutesWorked { get; set; }
    }
}
