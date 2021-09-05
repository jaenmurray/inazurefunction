using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace inazurefunction.Functions.Entities
{
    public class EmployeeEntity : TableEntity
    {
        public int IdEmployee { get; set; }

        public DateTime DateReg { get; set; }

        public int Type { get; set; }

        public bool IsConsolidated { get; set; }
    }
}
