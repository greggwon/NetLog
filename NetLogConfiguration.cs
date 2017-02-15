
using NetLog.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NetLogConfiguration
{
	public class ASPConsoleHandler: ConsoleHandler {
		public ASPConsoleHandler(string appName) {
			DefaultTraceListener def = new DefaultTraceListener();
			def.LogFileName = "c:/programdata/"+appName+"/Logs/Trace.log";
			//Trace.Listeners.Add(def);
		}
		public override void Publish( LogRecord rec ) {
			string msg = Formatter.format(rec);
			Trace.WriteLine(msg);
			Debug.WriteLine(msg);
			Console.Out.WriteLine(msg);
			Flush();
		}
		public override void Flush() {
			Trace.Flush();
			Debug.Flush();
			Console.Out.Flush();
		}
	}

	public class HistoryHandler: Handler {
		private static Queue<String> strm = new Queue<string>();
		private int historySize = 1000;

		public static List<String> LogHistory {
			get {
				return new List<string>(strm);
			}
		}
		public int HistorySize { get { return historySize; } set { historySize=value;}}

		public HistoryHandler() {
			StreamFormatter fmt = new StreamFormatter(false, true, false);
			fmt.Eol = "\n";
			Formatter = fmt;
			string str = ConfigurationManager.AppSettings[ "logHistorySize" ];
			if( str != null ) {
				historySize = int.Parse(str);
			}
		}

		public override void Close() {
		}

		public override void Flush() {
		}

		public override void Publish( LogRecord record ) {
			strm.Enqueue(Formatter.format(record));
//			if( strm.Count > historySize + historySize / 10 ) {
				while( strm.Count > historySize )
					strm.Dequeue();
//			}
		}
	}

    public class NetLog
    {
		private static Logger log = Logger.GetLogger(typeof(NetLog).FullName);
		public static void Setup( string logfileBaseName, string appName ) {
			String path = ConfigurationManager.AppSettings[ logfileBaseName+".log" ];
			if( path == null ) {
				path = "c:\\programdata\\"+appName+"\\Logs\\"+logfileBaseName+".log";
			}

			string appprops = "c:\\programdata\\" + appName + "\\" + logfileBaseName + ".properties";
			string genprops = "c:\\programdata\\" + appName + "\\logging.properties";
			string props = appprops;
			if( new FileInfo( props ).Exists == false && new FileInfo( genprops ).Exists ) {
				props = genprops;
			}

			System.Environment.SetEnvironmentVariable("netlog.logging.config.file", props );
			try {
				DirectoryInfo di = new DirectoryInfo(new FileInfo(path).DirectoryName);
				if( di.Exists == false )
					di.Create();
			} catch( Exception ex ) {
				log.severe(ex);
			}

			// If there is no visible logging properties file, setup a default config
			if( new FileInfo( props ).Exists == false ) {
				Logger top = Logger.GetLogger( "" );
				bool cfound = false, ffound=false, tfound=false, hfound = false;
				foreach( Handler h in top.GetHandlers() ) {
					if( h is ASPConsoleHandler ) {
						cfound = true;
					} else if( h is FileHandler ) {
						ffound = true;
					} else if( h is TCPSocketHandler ) {
						tfound = true;
					} else if( h is HistoryHandler ) {
						hfound = true;
					}
				}

				if( !tfound ) {
					int port = 12342;
					string portNo = ConfigurationManager.AppSettings[ "logServerPort" ];
					try {
						if( portNo != null ) {
							port = int.Parse( portNo );
						}
						TCPSocketHandler tcp = new TCPSocketHandler( port );
						tcp.Level = Level.ALL;
						top.AddHandler( tcp );
					} catch( Exception ex ) {
						Console.WriteLine( "Error setting up logging, tcp socket access problem for port " + port + " (config logServerPort=\"" + portNo + "\"): " + ex );
						log.severe( ex );
					}
				}

				if( !cfound ) {
					top.AddHandler( new ASPConsoleHandler( appName ) );
				}

				if( !hfound ) {
					top.AddHandler( new HistoryHandler() );
				}

				if( !ffound ) {
					try {
						FileHandler fh = new FileHandler( path );
						fh.Generations = 20;
						fh.Limit = 20000000;
						top.AddHandler( fh );
					} catch( Exception ex ) {
						Console.WriteLine( "Error setting up logging, log file access problem for \"" + path + "\": " + ex );
					}
				}
			}
		}
    }
}
