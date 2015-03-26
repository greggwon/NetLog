using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLog.NetLogMonitor {
	public class LogEntry : PropertyChangedBase {

		public LogEntry( string msg ) {
			Message = msg;
		}

		public string DateTime { get; set; }

		public string Index { get; set; }

		public string Message { get; set; }
	}
}
