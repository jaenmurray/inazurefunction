using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace inazurefunction.Functions.Entities
{
    public class ConsolidatedEntity : TableEntity
    {
        public int idEmployee { get; set; }

        public DateTime WorkedDate { get; set; }

        public int WorkedMinutes { get; set; }
    }
}
