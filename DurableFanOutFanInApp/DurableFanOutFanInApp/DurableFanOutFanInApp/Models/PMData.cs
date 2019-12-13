using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFanOutFanInApp.Models
{
    public class PMDataEntry
    {
        public DateTime Date { get; set; }
        public string Value { get; set; }
    }

    public class PMDataSource
    {
        public string Key { get; set; }

        public List<PMDataEntry> Values { get; set; }
    }

    public class PMDataOutput
    {
        public string Name { get; set; }
        public DateTime? LastUpdateDt { get; set; }
        public double LastUpdateValue { get; set; }
        public double AvgValue { get; set; }

    }
}
