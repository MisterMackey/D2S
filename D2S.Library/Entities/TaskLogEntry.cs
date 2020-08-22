using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Entities
{
    public class TaskLogEntry
    {
        [Key]
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string Target { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int RunId { get; set; }
        public virtual RunLogEntry Run { get; set; }
    }
}
