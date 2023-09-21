using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLog.Logging {
	public class EventLogFormatter : Formatter {
		public EventLogFormatter() {
		}

		public override string format ( LogRecord record ) {
			string str = "";
			str += record.LoggerName+"["+record.Level+"]: "+formatMessage(record);
			if( record.SourceClassName != null ) {
				str += "\n"+record.SourceClassName;
				if( record.SourceMethodName != null ) {
					str += "."+record.SourceMethodName+"(...)";
				}
			} else if( record.SourceMethodName != null ) {
				str += "\n"+record.SourceMethodName+"(...)";
			}
			if( record.Thrown != null ) {
				str += "\n"+record.Thrown.StackTrace;
			}
			return str;
		}
	}
}
