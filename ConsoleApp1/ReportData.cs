using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class ReportData
    {
        public long? workReportId { get; set; }
        public string reportType { get; set; }
        public int typeId { get; set; }
        public double? standardHours { get; set; }
        public double reportHour { get; set; }
        public string abnormalReportType { get; set; }
        public string remark { get; set; }
        public string reportTime { get; set; }
        public double? totalHours { get; set; }
        public string responsible { get; set; }
        public int reportUser { get; set; }
        public string creator { get; set; }
        public string createTime { get; set; }
        public int projectId { get; set; }
        public double reportedHour { get; set; }
        public double estimateHour { get; set; }
        public string challengeName { get; set; }
        public List<long> challengeId { get; set; }
        public string machineTool { get; set; }
        public long? gelId { get; set; }
        public int reportStatus { get; set; }
        public long taskId { get; set; }
    }
}