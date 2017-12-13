using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;

namespace NetLog.Logging {
	/// <summary>
	/// NetLog.Logging configuration details for logging to the system EventLog.
	/// Logfile should be set, before Source is set because only setting source will
	/// trigger automatic EventLog.Source creation, tying event logs to a particular
	/// logfile, which is "Application" by default.
	/// </summary>
	public class EventLogParms {
		private static string source;
		public static String LogName { get; set; }
		public static String Source {
			get { return source; }
			set {
				source = value;
				try {
					if( EventLog.SourceExists(source, System.Environment.MachineName ) == false ) {
						EventLog.CreateEventSource(source, LogName);
					}
				} catch( SecurityException ex ) {
					Console.WriteLine( ex );
					try {
						EventLog.CreateEventSource( source, LogName );
					} catch( Exception exx ) {
						Console.WriteLine( "Can not create source after security exception: "+ exx );
					}
				} catch( Exception ex ) {
					Console.WriteLine( ex );
				}
			}
		}

		static EventLogParms() {
			// LogName must be set first because setSource() refers to it.
			LogName = "Application";
			Source = "NetLogLogging";
		}
	}
}
