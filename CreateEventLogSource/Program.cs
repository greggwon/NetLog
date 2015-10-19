using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CreateEventLogSource {
	class Program {
		static void Main( string[] args ) {
			if( args.Length == 0 ) {
				Console.WriteLine("Usage: CreateEventLogSource [-d] source [logname]");
				return;
			}
			string source, log;
			bool delete = false;
			if( args[0] == "-d" ) {
				if( args.Length != 2 ) {
					Console.WriteLine("Usage: CreateEventLogSource [-d] source [logname]");
					return;
				}
				delete = true;
				source = args[1];
				log = null;
			} else {
				source = args[0];
				log = args.Length > 1 ? args[ 1 ] : "Application";
			}
			if( delete ) {
				EventLog.DeleteEventSource(source);
				Console.WriteLine("Deleted event source '" + source + "'");
			} else {
				if( EventLog.SourceExists(args[ 0 ]) ) {
					Console.WriteLine(args[ 0 ] + " event log source already exists");
				} else {
					EventLog.CreateEventSource(new EventSourceCreationData(args[ 0 ], log));
					Console.WriteLine("Created Source '" + args[ 0 ] + "' logging to '" + log + "'");
				}
			}
		}
	}
}
