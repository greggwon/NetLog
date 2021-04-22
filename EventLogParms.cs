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
		private static string source, logFile;
		public static String Logfile {
			get { return logFile; }
			set {
				logFile = value;
				SetupSourceLogfile();
			}
		}
		public static String Source {
			get { return source; }
			set {
				source = value;
				SetupSourceLogfile();
			}
		}
		private static void SetupSourceLogfile() {
			try {
				if( EventLog.SourceExists( source, "." ) == false ) {
					EventLog.CreateEventSource( source, Logfile );
				}
			} catch( SecurityException ex ) {
				Console.WriteLine( ex );
				try {
					EventLog.CreateEventSource( source, Logfile );
				} catch( Exception exx ) {
					Console.WriteLine( "Can not create source after security exception: " + exx );
				}
			} catch( Exception ex ) {
				Console.WriteLine( ex );
			}
		}
		static EventLogParms() {
			Source = "NetLog.Logging";
			Logfile = "Application";
		}
	}
}
