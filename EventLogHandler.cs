using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NetLog.Logging {
	public class EventLogHandler : Handler {
		private EventLog elog;
		private string machineName, source, logName;
		private int eventId = 0;
		private bool atTrace;

		public EventLogHandler() {
			Level = Level.EVENTLOG;
			string cmd = System.Environment.CommandLine;
			string[]arr = cmd.Split(new char[]{ ' ' } );
			arr = arr[0].Split(new char[] { '/','\\' } );
			cmd = arr[arr.Length-1];
			source = "App: "+cmd ;
			//Source = System.Environment.UserName+": @"+System.Environment.CommandLine;
			machineName = System.Environment.MachineName;
			Formatter = new EventLogFormatter();
			atTrace = true;
			logName = cmd;
//			Console.WriteLine( "source is \"" + Source + "\", from arr[]=" + arr );
			elog = new EventLog( LogName, MachineName, Source );
		}

		public bool IncludeAtStacktrace {
			get{ return atTrace; }
			set { atTrace = value; }
		}

		public string MachineName {
			get { return machineName; }
			set { 
				machineName = value;
				elog = new EventLog( LogName, MachineName, Source );
			}
		}
		public string Source {
			get { return source; }
			set { 
				source = value;
				elog = new EventLog( LogName, MachineName, Source );
			}
		}

		public string LogName {
			get { return logName; }
			set { 
				logName = value;
				elog = new EventLog( LogName, MachineName, Source );
			}
		}

		public override void Flush ( ) { }
		public override void Close() {}
		public override void Publish( LogRecord record ) {
			// stop now if not loggable
			if ( record.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF ) {
				return;
			}
			Enqueue( record );
		}

		protected override void Push(LogRecord record) {

			if( EventLog.SourceExists( Source ) == false && LogName != null ) {
				EventLog.CreateEventSource( Source, LogName );
			}
			string str = Formatter.format( record );
			if( IncludeAtStacktrace ) {
				str += "\n\nAt:\n"+ System.Environment.StackTrace;
			}
			EventLogEntryType type = record.Level.IntValue >= Level.SEVERE.IntValue ?
				EventLogEntryType.Error
				: record.Level.IntValue >= Level.WARNING.IntValue ?
				EventLogEntryType.Warning
				: EventLogEntryType.Information;
			elog.WriteEntry( str, type, eventId++ ) ;
		}
	}
}
