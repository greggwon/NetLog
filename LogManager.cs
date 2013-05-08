using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Reflection;

namespace NetLog.Logging
{
	public class LogManager
	{
		private static Dictionary<string, Logger> loggers = new Dictionary<string, Logger>();
		private static Dictionary<string, Handler> handlers = new Dictionary<string, Handler>();
		private static Dictionary<string, Formatter> formatters = new Dictionary<string, Formatter>();
		private static Dictionary<string, Level> levels = new Dictionary<string, Level>();
		private static LogManager mgr;

		private static Boolean configLoaded;
		private bool consoleDebug;

		public bool ConsoleDebug {
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		internal static Dictionary<string,Logger> Loggers {
			get { return loggers; }
		}

		public void ReadConfiguration() {
			try
			{
				String props = System.Environment.GetEnvironmentVariable("netlog.logging.properties");
				if( props == null )
					props = "logging.properties";
				if( new FileInfo( props ).Exists == false ) {
					if( consoleDebug ) {
						Console.WriteLine("The designated properties file: "+props+" does not exist" );
					}
					return;
				}
				using ( StreamReader sr = new StreamReader( props ) )
				{
					try {
						configLoaded = true;
						readStream(sr);
					} finally {
						sr.Close();
					}
				}
			} catch (Exception e) {
				configLoaded = false;
				Console.WriteLine("The Logging properties file could not be read:");
				Console.WriteLine("# SEVERE # "+e.Message+": "+e.StackTrace);
			}			
		}

		public void ReadConfiguration( StreamReader rd ) {
			configLoaded = true;
			readStream(rd);
		}

		private void readStream( StreamReader rd ) {
			try {
				string line;
				while( ( line = rd.ReadLine() ) != null ) {
					int idx;
					if( line.StartsWith("handlers=" ) ) {
						string[]arr = line.Substring( "handlers=".Length ).Split( new char[]{','} ) ;
						if( arr.Count() > 0 )
							Logger.getLogger("").GetHandlers().Clear();
						foreach( string cls in arr ) {
							Handler h = (Handler)Activator.CreateInstance( Type.GetType(cls) );
							if( h == null ) {
								Console.WriteLine("Can't load Handler class: "+cls );
								continue;
							}
							handlers[cls] = h;
							if( consoleDebug )
								Console.WriteLine("adding handler: "+cls );
							Logger.getLogger("").GetHandlers().Add( h );
						}
						if( Logger.getLogger("").GetHandlers().Count == 0 ) {
							// put back a console handler if the handlers could not be loaded
							Logger.getLogger("").AddHandler( new ConsoleHandler() );
						}
						if (consoleDebug)
							Console.WriteLine("handlers now (" + Logger.getLogger("").GetHandlers().Count + ") :" + Logger.getLogger("").GetHandlers()[0]);
					} else if( ( idx = line.IndexOf(".formatter=" ) ) >= 0 ) {
						String handler = line.Substring( 0, idx );
						string[]arr = line.Substring( idx + ".formatter=".Length ).Split( new char[]{','} ) ;
						// we don't really need to do split and loop, because only one formatter is used,
						// but we will code it this way to allow the last formatter to be used so that a
						// configuration file might contain multiple formatters and just moving one to the end
						// will use it.
						foreach( string cls in arr ) {
							if( ! handlers.ContainsKey( handler ) ) {
								if (consoleDebug)
									Console.WriteLine("Can't set formatter on nonexistent handler: \""+handler+"\"" );
								continue;
							}
							Handler h = handlers[handler];
							if (consoleDebug)
								Console.WriteLine("set formatter on handler \"" + handler + "\": " + h);
							Formatter f = (Formatter)Activator.CreateInstance(Type.GetType(cls));
							// set the formatter.
							if (consoleDebug)
								Console.WriteLine("setting formatter: " + cls);
							h.Formatter = f;
						}
					} else if( line.StartsWith("config=") ) {
						string[]arr = line.Substring("config=".Length).Split( new char[]{','} ) ;
						foreach( string cls in arr ) {
							if (consoleDebug)
								Console.WriteLine("instantiating config: " + cls);
							Activator.CreateInstance(Type.GetType(cls));
						}
					} else if( line.Contains(".level=") ) {
						String name = line.Substring( 0, line.IndexOf(".level=") );
						String level = line.Substring( line.IndexOf(".level=")+".level=".Length );
						Level l = Level.parse( level );
						levels[name] = l;
						if( handlers.ContainsKey( name ) ) {
							handlers[name].Level = l;
						} else {
							Logger.getLogger( name ).Level = l;
						}
					} else {
						string[]arr = line.Split(new char[]{ '=' } );
						if( arr.Length == 2 ) {
							string[] nm = arr[0].Split( new char[]{ '.' } );
							string cls = "";
							for( int i = 0; i < nm.Length-1; ++i ) {
								if( cls.Length == 0 ) {
									cls = nm[i];
								} else {
									cls = cls + "." + nm[i];
								}
							}
							if (consoleDebug)
								Console.WriteLine("found property name: " + arr[0] + " and value =" + arr[1]);
							if( handlers.ContainsKey( cls ) ) {
								string propnm = nm[ nm.Length-1 ];
								if (consoleDebug)
									Console.WriteLine("found handler property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
								Handler h = handlers[cls];
								putPropValue( h, propnm, arr[1] );
							} else if( formatters.ContainsKey( cls ) ) {
								string propnm = nm[nm.Length - 1];
								if (consoleDebug)
									Console.WriteLine("found handler property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
								Formatter f = formatters[cls];
								putPropValue( f, propnm, arr[1] );
							}			
						}
					}
				}
			}
			catch (Exception e)
			{
				if (consoleDebug)
					Console.WriteLine("The Logging configuration stream could not be read:");
				Console.WriteLine(e.Message+": "+e.StackTrace);
			}
		}

		private void putPropValue(object obj, string propnm, string value)
		{
			Type type = obj.GetType();
			PropertyInfo propInfo = type.GetProperty(propnm);
			if( propInfo.PropertyType == typeof(Int32) ) {
				propInfo.SetValue(obj, int.Parse(value), null);
			} else if( propInfo.PropertyType == typeof(long)) {
				propInfo.SetValue(obj, long.Parse(value), null);
			}
			else if (propInfo.PropertyType == typeof(float))
			{
				propInfo.SetValue(obj, float.Parse(value), null);
			}
			else if (propInfo.PropertyType == typeof(bool))
			{
				propInfo.SetValue(obj, bool.Parse(value), null);
			}
			else if (propInfo.PropertyType == typeof(string))
			{
				propInfo.SetValue(obj, value, null);
			} else {
				throw new System.ArgumentException("don't konw how to set property type "+propInfo.PropertyType );
			}
		}

		public void reset() {
			lock( loggers ) {
				loggers.Clear();
			}
		}

		public static LogManager GetLogManager() {
			lock( loggers ) {
				if( mgr == null ) {
					mgr = new LogManager();
					mgr.ReadConfiguration();
				}
				return mgr;
			}
		}

		public List<string> LoggerNames {
			get {
				List<string> names = new List<string>(loggers.Keys);
				return names;
			}
		}

		public Boolean AddLogger( Logger logger ) {
			lock(loggers) {
				if( !configLoaded ) {
					ReadConfiguration();
				}
				Boolean have = loggers.ContainsKey(logger.Name);
				loggers[logger.Name] = logger;

				return have;
			}
		}
	}
}
