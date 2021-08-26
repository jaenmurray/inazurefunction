using System;

namespace inazurefunction.Common.Models
{
    public class EmployeeReg
    {
        public int? IdEmployee { get; set; }

        public DateTime DateReg { get; set; }

        public int? Type { get; set; }

        public bool IsConsolidated { get; set; }
    }
}
