using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
namespace NetLog.Logging
{
	public class LoggingConfiguration {
		public string LogManager { get { return LogManager; } }
	}
	public class LogManager
	{
		private static volatile ConcurrentDictionary<string, Logger> loggers = new ConcurrentDictionary<string, Logger>();
		private static volatile ConcurrentDictionary<string, Handler> handlers = new ConcurrentDictionary<string, Handler>();
		private static volatile ConcurrentDictionary<string, Filter> filters = new ConcurrentDictionary<string, Filter>();
		private static volatile ConcurrentDictionary<string, Formatter> formatters = new ConcurrentDictionary<string, Formatter>();
		private static volatile ConcurrentDictionary<string, Level> levels = new ConcurrentDictionary<string, Level>();
		private static volatile LogManager mgr;
		private static bool primordialInit;
		private static volatile FileSystemWatcher fw;
		private static Logger log;
		DateTime? lastModifiedConfig;
		private static Boolean configLoaded;

		static LogManager() {
			string instName = System.Environment.GetEnvironmentVariable( "netlog.logging.logmanager" );
			if( instName != null ) {
				mgr = (LogManager)Activator.CreateInstance( Type.GetType( instName ) );
			} else {
				mgr = new LogManager();
				mgr.ReadConfiguration();
			}

			// this will force configuration load, and errors there can not expect "log" to be initialized yet.
			log = Logger.GetLogger( "NetLog.Logging.LogManager" );
		}

		public bool ConsoleDebug {
			get { return log != null && log.IsLoggable(Level.FINE); }
			set { log.Level = value ? Level.FINE : Level.INFO ; }
		}

		internal static IDictionary<string,Logger> Loggers {
			get { return loggers; }
		}

		private void OnChangedConfig ( object source, FileSystemEventArgs args ) {
			Console.WriteLine( "logging.properties file "+args.ChangeType+": " + args.FullPath );
			lock ( loggers ) {
				if( args.ChangeType == WatcherChangeTypes.Changed || args.ChangeType == WatcherChangeTypes.Created ) {
					mgr.ReadConfiguration();
				}
			}			
		}

		public void ReadConfiguration() {
			if( false )
				Console.WriteLine( "Reading Configuration: " + Environment.StackTrace );
			string initName = System.Environment.GetEnvironmentVariable( "netlog.logging.config.class" );
			string initAsmb = System.Environment.GetEnvironmentVariable( "netlog.logging.config.assembly" );
			if ( initName != null ) {
				if( primordialInit )
					return;
				primordialInit = true;
				// Now, create an instance of the class that initName specifies,
				// to allow initialization to run from its constructor's actions.
				try {
					int idx = initName.LastIndexOf(".");
					string assemb = initAsmb;
					if( initAsmb == null )
						assemb = initName.Substring(0,idx);
					string name = initName.Substring( idx+1 );
					// Create an instance to run the constructor
					Activator.CreateInstance( assemb, initName ).Unwrap();
					return;

				} catch ( System.IO.FileNotFoundException ex ) {
					Console.WriteLine( "# SEVERE # Error insantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": " + ex );//+":\n"+ex.StackTrace	);
					ReportExceptionToEventLog("# SEVERE # Error insantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": ", ex);
					// Continue and do normal initialization.
				} catch ( System.TypeLoadException ex ) {
					Console.WriteLine("# SEVERE # Error insantiating \"netlog.logging.config.class\" "+
						"specified initialization class, \""+initName+"\": "+ex );//+":\n"+ex.StackTrace	);
					// Continue and do normal initialization.
					ReportExceptionToEventLog("# SEVERE # Error insantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": ", ex);
				}
			}
			try {
				NetLogConfigurationSection sect = (NetLogConfigurationSection)
					ConfigurationManager.GetSection("LoggingConfiguration");
				if( sect != null ) {
					ActivateConfigurationSection(sect);
					configLoaded = true;
					return;
				}
			} catch( Exception ex ) {
				Console.WriteLine(ex.Message + ":\n" + ex);
			}

			String props = "N/A";
			try {
				props = System.Environment.GetEnvironmentVariable( "netlog.logging.config.file" );
				if( props == null )
					props = ConfigurationManager.AppSettings["netlog.logging.config.file"];
				if( props == null )
					props = "logging.properties";
				if( fw != null ) {
					if( log != null && log.IsLoggable(Level.FINE) )
						Console.WriteLine( "Dropping existing watcher for: " + fw.Path );
					fw.EnableRaisingEvents = false;
				}
				fw = new FileSystemWatcher();
				fw.Path = new FileInfo( props ).DirectoryName;
				fw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;// | NotifyFilters.DirectoryName;
				fw.Changed += new FileSystemEventHandler( OnChangedConfig );
				fw.Filter = new FileInfo( props ).Name;
				if( new FileInfo( props ).Exists == false ) {
					if( log != null && log.IsLoggable( Level.FINE ) ) {
						Console.WriteLine( "The designated properties file: " + props + " does not exist in " +
							Directory.GetCurrentDirectory() );
					}
					return;
				}
				Exception ex = null;
				bool readIt = false;
				for( int i = 0; !readIt && i < 3; ++i ) {
					try {
						using( StreamReader sr = new StreamReader( props ) ) {
							Console.WriteLine( "Opening " + props + " returned " + sr );
							configLoaded = true;
							readStream( sr );
							readIt = true;
						}
						break;
					} catch( Exception e ) {
						ReportExceptionToEventLog( "Exception opening configuration from " + props + ", attempt #" + ( i + 1 ), e );
						configLoaded = false;
						if( ex == null )
							ex = e;
					}
				}
				if( !readIt ) {
					configLoaded = false;
					Console.WriteLine( "The Logging properties file, \"" + props + "\", could not be read!" );
					Console.WriteLine( "# SEVERE # " + ex.Message + ": " + ex.StackTrace );
					ReportExceptionToEventLog( "The Logging properties file, \"" + props + "\", could not be read!", ex );
				}
			} catch( Exception e ) {
				configLoaded = false;
				ReportExceptionToEventLog( "The Logging properties file, \"" + props + "\", could not be read!", e );
				Console.WriteLine( "The Logging properties file, \"" + props + "\", could not be read!" );
				Console.WriteLine( "# SEVERE # " + e.Message + ": " + e.StackTrace );
			} finally {
				if( fw != null )
					fw.EnableRaisingEvents = true;
			}
		}

		private void ActivateConfigurationSection( NetLogConfigurationSection sect ) {
			foreach( FormatterConfiguration fmt in sect.Formatters ) {
				string name = fmt.Formatter;
				string cls = fmt.ClassName;
				string asmb = fmt.AssemblyName;
				Formatter fmtr = (Formatter)Activator.CreateInstance(asmb.Length == 0 ? null : asmb, cls).Unwrap();
				formatters[name] = fmtr;
				foreach( PropertyConfiguration p in fmt.Properties ) {
					SetPropertyOn(fmtr, p);
				}
			}
			foreach( FilterConfiguration filtcfg in sect.Filters ) {
				string name = filtcfg.Filter;
				string cls = filtcfg.ClassName;
				string asmb = filtcfg.AssemblyName;
				Filter filt = (Filter)Activator.CreateInstance(asmb.Length == 0 ? null : asmb, cls).Unwrap();
				filters[name] = filt;
				foreach( PropertyConfiguration p in filtcfg.Properties ) {
					SetPropertyOn(filt, p);
				}
			}
			foreach( HandlerConfiguration hnd in sect.Handlers ) {
				string name = hnd.Handler;
				string fmt = hnd.Formatter;
				string cls = hnd.ClassName;
				string asmb = hnd.AssemblyName;
				Handler hndlr = (Handler)Activator.CreateInstance(asmb.Length == 0 ? null : asmb, cls).Unwrap();
				if( formatters.ContainsKey( fmt ) ) {
					hndlr.Formatter = formatters[fmt];
				} else if( fmt.Equals("streamFormatter") ) {
					hndlr.Formatter = new StreamFormatter(false, true, false);
					formatters[fmt] = hndlr.Formatter;
				}
				foreach( PropertyConfiguration p in hnd.Properties ) {
					SetPropertyOn(hndlr, p);
				}
				handlers[name] = hndlr;
			}
			foreach( LoggerConfiguration lg in sect.Loggers ) {
				string name = lg.Name;
				string handler = lg.Handler;
				Logger log;
				if( lg.ClassName == null || lg.ClassName.Length == 0 || loggers.ContainsKey(name) ) {
					if( loggers.ContainsKey(name) ) {
						throw new InvalidOperationException("Logger with name " + name + " is already defined, can't instantiate as \"" + lg.ClassName + "\"");
					}
					log = Logger.NeedNewLogger(name);
				} else {
					string asmb = lg.AssemblyName;
					log = (Logger)Activator.CreateInstance(asmb.Length == 0 ? null : asmb, lg.ClassName).Unwrap();
				}
				loggers[ name ] = log;
				if( handlers.ContainsKey(handler) ) {
					log.AddHandler(handlers[ handler ]);
				}
				foreach( PropertyConfiguration p in lg.Properties ) {
					SetPropertyOn(log, p);
				}
			}
		}

		private void SetPropertyOn( object fmtr, PropertyConfiguration p ) {
			putPropValue(fmtr, p.Name, p.Value);
		}

		public static void ReportExceptionToEventLog( string msg, Exception ex ) {
			string str = "";
			try {
				if( ex != null ) {
					if( EventLog.SourceExists(EventLogParms.Source, ".") == false ) {
						EventLog.CreateEventSource(EventLogParms.Source, "Application");
					}
					str = ExpandStackTraceFor(ex);
				}
				EventLog.WriteEntry(EventLogParms.Source, msg + ( ex == null ? "" : ( ": " + str ) ), EventLogEntryType.Error, 0, -1);
			} catch( Exception exx ) {
				Console.WriteLine("Error writing to eventLog for exception: " + exx );
				Console.WriteLine(msg + ": " + ex.Message+"\n"+ex);
			}
		}

		private static string ExpandStackTraceFor( Exception ex ) {
			string str = "";
			if( ex.InnerException != null ) {
				str += "Inner: "+ExpandStackTraceFor(ex.InnerException);
			}
			str += ex.GetType().FullName + ": " + ex.Message;
			str += ex.StackTrace;
			return str;
		}


		public void ReadConfiguration( StreamReader rd ) {
			configLoaded = true;
			readStream(rd);
		}

		internal class CachedLevel {
			DateTime when;
			internal Level level;
			public CachedLevel( Level l ) {
				level = l;
				when = DateTime.UtcNow;
			}
			public bool Expired {
				get {
					return DateTime.UtcNow - when > new TimeSpan( 0, 0, 10 );
				}
			}
		}

		private ConcurrentDictionary<string,CachedLevel> cachedLevels = new ConcurrentDictionary<string, CachedLevel>();

		public Level LevelOfLogger( string name ) {
			if( levels.ContainsKey(name) ) {
				return levels[ name ];
			}
			if( cachedLevels.ContainsKey( name ) && cachedLevels[ name ].Expired == false )
				return cachedLevels[ name ].level;
			//"logger.name.more.other"

			//	"logger", "name", "more", "other"
			string nm = name;
			//string[]arr = name.Split('.');
			while( nm.LastIndexOf('.') >= 0 ) {
				int idx = nm.LastIndexOf( '.' );
				nm = nm.Substring( 0, idx - 1 );

				// If there is a physical Logger at this level, use any
				// level explicitly set there.
				if( loggers.ContainsKey(nm) && loggers[ nm ].level != null ) {
					Level l = loggers[ nm ].level;
					cachedLevels[ nm ] = new CachedLevel( l );
					return l;
				}
				// If there is a property set Level at a particular level, use that
				// level.
				if( levels.ContainsKey(nm) ) {
					Level l = levels[ nm ];
					cachedLevels[ nm ] = new CachedLevel( l );
					return l;
				}
			}
			try {
				// The top level logger has a level set for itself.
				// technically the loop above also covers this logger name
				// but we'll do it explicitly here too just to make sure.
				if( loggers.ContainsKey( "" ) ) {
					Level l =  loggers[ "" ].Level;
					cachedLevels[ "" ] = new CachedLevel( l );
					return l;
				}
			} catch( Exception ex ) {
				ReportExceptionToEventLog("Can't get level for '' Logger instance", ex);
			}
			return Level.ALL;
		}

		private void readStream( StreamReader rd ) {
			try {
				string line;
				while( ( line = rd.ReadLine() ) != null ) {
					int idx;
					if( line.StartsWith("handlers=" ) ) {
						Dictionary<string, Handler> oh = new Dictionary<string, Handler>();
						string[]arr = line.Substring( "handlers=".Length ).Split( new char[]{','} ) ;
						if( arr.Count() > 0 )
							Logger.GetLogger("").GetHandlers().Clear();
						foreach( string cls in arr ) {
							Handler h;
							if( handlers.ContainsKey( cls ) == false ) {
								try {
									h = (Handler)Activator.CreateInstance( Type.GetType(cls) );
								} catch( Exception ex ) {
									ReportExceptionToEventLog("Error creating logging Handler \"" + cls + "\"", ex);
									Console.WriteLine( "# ERROR # Error creating handler \""+cls+"\": "+ex.Message+"\n"+ex.Source+": "+ex.StackTrace );
									Exception other = ex.InnerException;
									while( other != null ) {
										Console.WriteLine("InnerException:\n" + other.StackTrace);
										other = other.InnerException;
									}
									continue;
								}
								if( h == null ) {
									Console.WriteLine("Can't load Handler class: "+cls );
									continue;
								}
								handlers[cls] = h;
							} else {
								h = handlers[cls];
							}
							if( log != null && log.IsLoggable(Level.FINE) )
								Console.WriteLine( "adding handler: " + cls );
							Logger.GetLogger( "" ).GetHandlers( ).Add( h );
							// rememeber which handlers are in the root logger.
							oh[cls] = h;
						}
						// remove any handlers no longer listed.
						if( log != null && log.IsLoggable(Level.FINE) )
							Console.WriteLine( "root handlers: " + Logger.GetLogger( "" ).GetHandlers( ).Count );
						foreach( Handler hh in new List<Handler>( Logger.GetLogger("").GetHandlers() ) ) {
							if( log != null && log.IsLoggable(Level.FINE) )
								Console.WriteLine( "Checking if using handler: \"" + hh.GetType( ).FullName + "\": " + oh );
							if ( oh.ContainsKey( hh.GetType( ).FullName ) == false ) {
								Logger.GetLogger("").RemoveHandler( hh );
								if( log != null && log.IsLoggable(Level.FINE) )
									Console.WriteLine( "Removing no longer used handler: \"" + hh.GetType( ).FullName + "\"" );
							}
						}

						// makes sure that at least a console handler is active if nothing else.
						if( Logger.GetLogger("").GetHandlers().Count == 0 ) {
							// put back a console handler if the handlers could not be loaded
							Handler h = GetDefaultHandler();
							if ( handlers.ContainsKey( h.GetType( ).FullName ) == false ) {
								handlers[h.GetType( ).FullName] = h;
							}
							Logger.GetLogger("").AddHandler( h );
						}
						if( log != null && log.IsLoggable(Level.FINE) )
							Console.WriteLine("handlers now (" + Logger.GetLogger("").GetHandlers().Count + ") :" + Logger.GetLogger("").GetHandlers()[0]);
					} else if( ( idx = line.IndexOf(".formatter=" ) ) >= 0 ) {
						String handler = line.Substring( 0, idx );
						string[]arr = line.Substring( idx + ".formatter=".Length ).Split( new char[]{','} ) ;
						// we don't really need to do split and loop, because only one formatter is used,
						// but we will code it this way to allow the last formatter to be used so that a
						// configuration file might contain multiple formatters and just moving one to the end
						// will use it.

						foreach( string cls in arr ) {
							if( ! handlers.ContainsKey( handler ) ) {
								if( log != null && log.IsLoggable(Level.FINE) )
									Console.WriteLine("Can't set formatter on nonexistent handler: \""+handler+"\"" );
								continue;
							}
							Handler h = handlers[handler];
							if( log != null && log.IsLoggable(Level.FINE) )
								Console.WriteLine("set formatter on handler \"" + handler + "\": " + h);
							lock( formatters ) {
								if( formatters.ContainsKey( cls ) == false ) {
									formatters[ cls ] = (Formatter)Activator.CreateInstance( Type.GetType( cls ) );
								} else {
									if( log.IsLoggable( Level.FINE ) )
										Console.WriteLine( "Already have formatter for class: " + cls );
								}
							}
							Formatter f = formatters[ cls ];
							// set the formatter.
							if( log != null && log.IsLoggable(Level.FINE) )
								Console.WriteLine("setting formatter: " + cls);
							h.Formatter = f;
						}
					} else if( line.StartsWith("config=") ) {
						string[]arr = line.Substring("config=".Length).Split( new char[]{','} ) ;
						foreach( string cls in arr ) {
							if( log != null && log.IsLoggable(Level.FINE) )
								Console.WriteLine("instantiating config: " + cls);
							Activator.CreateInstance(Type.GetType(cls));
						}
					} else if ( line.Contains( ".level=" ) || line.Contains( ".Level=" ) ) {
						String name;
						int lidx;
						if( line.Contains( ".level=" ) )
							name = line.Substring( 0, lidx= line.IndexOf(".level=") );
						else // if( line.Contains( ".Level=" ) )
							name = line.Substring( 0, lidx=line.IndexOf(".Level=") );

						String level = line.Substring( lidx+(".level=".Length) );
						Level l = Level.parse( level );
						PutLevel(name, l);
						if( ConsoleDebug )
							Console.WriteLine("set level for \"" + name + "\" to " + l);

						if( handlers.ContainsKey( name ) ) {
							handlers[name].Level = l;
						} else {
							Logger lg = Logger.GetLogger(name);
							lg.Level = l;
							if( ConsoleDebug )
								Console.WriteLine("set logger level for \"" + lg.Name + "\" to " + lg.Level);
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
							if( log != null && log.IsLoggable(Level.FINE) )
								Console.WriteLine("found property name: " + arr[0] + " and value =" + arr[1]);
							try {
								if( handlers.ContainsKey( cls ) ) {
									string propnm = nm[ nm.Length-1 ];
									if( log != null && log.IsLoggable(Level.FINE) )
										Console.WriteLine("found handler property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
									Handler h = handlers[cls];
									if( h != null ) {
										putPropValue( h, propnm, arr[1] );
									} else {
										Console.WriteLine("# ERROR # Handler property value reference to unused handler class: "+cls);
									}
								} else if( formatters.ContainsKey( cls ) ) {
									string propnm = nm[nm.Length - 1];
									if( log != null && log.IsLoggable(Level.FINE) )
										Console.WriteLine("found handler property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
									Formatter f = formatters[cls];
									putPropValue( f, propnm, arr[1] );
								}
							} catch( Exception ex ) {
								ReportExceptionToEventLog("Can't set property value: \"" + line + "\"", ex);
								Console.WriteLine("# ERROR # can't set property value: \"" + line + "\": " + ex + "\n" + ex.StackTrace);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				// log will be null during initial configuration load, so log those errors
				if( log == null || log.IsLoggable(Level.FINE) ) {
					Console.WriteLine();
				}
				ReportExceptionToEventLog("The Logging configuration stream could not be read", e);
				Console.WriteLine("# SEVERE # "+e.Message+":\n"+e.StackTrace);
			}
		}

		private void PutLevel( string name, Level l ) {
			levels[ name ] = l;
		}

		protected void putPropValue(object obj, string propnm, string value)
		{
			Type type = obj.GetType();
			PropertyInfo propInfo = type.GetProperty(propnm);
			if( propInfo == null ) {
				throw new System.ArgumentException("Can't find property named " + propnm+" for "+obj.GetType().FullName );
			}

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
			if( mgr != null )
				return mgr;
			lock( loggers ) {
				if ( mgr == null ) {
					mgr = new LogManager();
					mgr.ReadConfiguration( );
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

		internal static void ConsoleWriteLine( string p, string logger ) {
			if( p.Length == 0 ) {
				Console.WriteLine("");
				return;
			}
			
			Console.Write(DateTime.Now.ToString(StreamFormatter.fmt));
			Console.Write(" [" + logger + "]");
			Console.Write(" CONSOLE # ");
			Console.WriteLine(p);
		}

		internal static void ConsoleWrite( string p ) {
			Console.Write(p);
		}

		internal Handler GetDefaultHandler() {
			String nm = typeof( ConsoleHandler ).FullName;
			lock( handlers ) {
				if( handlers.ContainsKey( nm ) ) {
					return handlers[ nm ];
				}
				ConsoleHandler h = new ConsoleHandler();
				handlers[ nm ] = h;
				return h;
			}
		}
	}
}
