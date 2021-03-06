﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Entities
{
    public class RunLogEntry
    {
        [Key]
        public int RunId { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string MachineName { get; set; }

        public virtual ICollection<TaskLogEntry> Tasks { get; set; }
    }
}
