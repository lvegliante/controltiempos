using System;
using System.Collections.Generic;
using System.Text;

namespace controltiempos.Common.Models
{
    public class InputOutput
    {
        public int EmployeeId { get; set; }
        public DateTime InputOrOutput { get; set; }
        public int Type { get; set; }
        public bool IsConsolidated { get; set; }

    }
}
