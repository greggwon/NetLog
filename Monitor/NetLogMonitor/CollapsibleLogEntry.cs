using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLog.NetLogMonitor {
	public class CollapsibleLogEntry : LogEntry {
		public CollapsibleLogEntry( string msg )
			: base( msg ) {
		}
		public List<LogEntry> Contents { get; set; }
	}
}
