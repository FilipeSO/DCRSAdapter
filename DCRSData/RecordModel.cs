using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCRSDataLayer
{
    public class RecordModel
    {
        public DateTime RecordStart { get; set; }
        public DateTime RecordEnd { get; set; }
        public int Duration { get; set; }
        public string AgentName { get; set; }
        public FileInfo RecordFile { get; set; }
    }
}
