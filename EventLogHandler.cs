using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace NetLog.Logging {
	/// <summary>
	/// This Handler write log entries to the eventlog logfile which is configured
	/// with it's properties, or at creation time with one of the appropriate constructors.
	/// If a "source" is not provided, the last element of the current directory path is used
	/// for the source, since that would typically be the installation location of the application
	/// and would be some form of the application name.
	/// </summary>
	public class EventLogHandler : Handler, IDisposable {
		private EventLog elog;
		private string machineName, source, logName;
		private int eventId = 0;
		private bool atTrace;
		private static bool haveEventLog;

		public EventLogHandler( String source, String logName )
			: base() {
				PrepareLogging(source, logName);
		}
		public EventLogHandler() {
			Level = Level.EVENTLOG;

			// create an event log configuration for our own diagnostics to go into.
			try {
				if( EventLog.SourceExists("NetLog.Logging") == false ) {
					EventLog.CreateEventSource("NetLog.Logging","NetLog.Logging.EventLog");
				}
				haveEventLog = true;
			} catch( Exception ex ) {
				haveEventLog = false;
				reportInternalException( "Error setting up NetLog.Logging event source "+
					"for logging to NetLog.Logging.EventLog", ex );
			}
			string cmd = System.Environment.CommandLine;
			// space separated command line, take first element?
			string[]arr = cmd.Split(new char[]{ ' ' } );
			// split path into components
			arr = arr[0].Split(new char[] { '/','\\' } );
			// Take last element of path
			cmd = arr[ arr.Length - 1 ];

			// use the last element of the directory path for the name by default.
			FileInfo f = new FileInfo(System.Environment.CurrentDirectory);
			source = f.Name;
			source = source.Replace("-", "");
			source = source.Replace("_", "");
			source = source.Replace(".", "_");

			PrepareLogging(source, cmd);
		}

		private void reportInternalException( string msg, Exception ex ) {
			if( !haveEventLog ) {
				Console.WriteLine(msg+": "+ex);
				Console.WriteLine(ex.StackTrace);
			} else {
				EventLog.WriteEntry(NetLog.SOURCE, msg + ": " + ex + "\n" + ex.StackTrace, EventLogEntryType.Error);
			}
		}

		void PrepareLogging( String source, String logName ) {
			machineName = System.Environment.MachineName;
			Formatter = new EventLogFormatter();
			atTrace = true;
			this.logName = logName;
			EstablishHandlerSource(source);
			this.source = source;
		}

		private void EstablishHandlerSource(string source) {
			if( !System.Diagnostics.EventLog.SourceExists(source) ) {
				System.Diagnostics.EventLog.CreateEventSource(
					source, LogName);
			}
			elog = new EventLog(LogName, MachineName, source);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		void Dispose( bool all ) {
			elog.Dispose();
			elog = null;
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
				// always delete an existing source because it may be changing
				try {
					if( System.Diagnostics.EventLog.SourceExists(this.source) ) {
						System.Diagnostics.EventLog.DeleteEventSource(this.source);
					}
					EstablishHandlerSource(value);
					source = value;
				} catch( Exception ex ) {
					reportInternalException("Can not use/create source \"" + source + "\"", ex);
				}
			}
		}

		public string LogName {
			get { return logName; }
			set { 
				logName = value;
				EstablishHandlerSource(source);
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
			if( EventLog.SourceExists(Source) == false && LogName != null ) {
				EventLog.CreateEventSource( Source, LogName );
			} else if( EventLog.LogNameFromSourceName(Source, machineName).Equals(LogName) == false ) {
				EventLog.DeleteEventSource(Source);
				EventLog.CreateEventSource(Source, LogName);
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
			string sub = str;
			while( sub.Length > 0 ) {
				if( sub.Length > 31839 ) {
					elog.WriteEntry( sub.Substring( 0, 31839 ), type, eventId++ );
					sub = sub.Substring( 31839 );
				} else {
					elog.WriteEntry(sub, type, eventId++);
					sub = "";
				}
			}
		}
	}
}
