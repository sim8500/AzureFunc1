
using Microsoft.WindowsAzure.Storage.Table;

namespace TestGiosFuncApp.Models
{
    class WarningEntity : TableEntity
    {
        public WarningEntity(string date, string time)
        {
            PartitionKey = date;
            RowKey = time;
        }

        public string Text { get; set; }
    }
}
