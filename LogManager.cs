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
		private static volatile Dictionary<string, Logger> loggers = new Dictionary<string, Logger>();
		private static volatile ConcurrentDictionary<string, Handler> handlers = new ConcurrentDictionary<string, Handler>();
		private static volatile ConcurrentDictionary<string, Filter> filters = new ConcurrentDictionary<string, Filter>();
		private static volatile ConcurrentDictionary<string, Formatter> formatters = new ConcurrentDictionary<string, Formatter>();
		private static volatile Dictionary<string, Level> levels = new Dictionary<string, Level>();
		private static volatile LogManager mgr;
		private static volatile bool primordialInit;
		private static volatile FileSystemWatcher fw;
		private static Logger log;
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
			ConsoleWriteLine( "logging.properties file "+args.ChangeType+": " + args.FullPath );
			lock ( loggers ) {
				if( args.ChangeType == WatcherChangeTypes.Changed || args.ChangeType == WatcherChangeTypes.Created ) {
					mgr.ReadConfiguration();
				}
			}			
		}

		private static StreamWriter sw;
		private static FileInfo tmplog;
		private static String tmpfile;
		public void ReadConfiguration() {
			tmpfile = Path.GetTempPath() + "/NetLogTrace.log";
			tmplog = new FileInfo( tmpfile );
			ConsoleWriteLine( "Reading Configuration: ("+tmpfile+(tmplog.Exists?" enabled" : " missing")+") called from:\n" + Environment.StackTrace );
			string initName = null;// System.Environment.GetEnvironmentVariable( "netlog.logging.config.class" );
			string initAsmb = null;// System.Environment.GetEnvironmentVariable( "netlog.logging.config.assembly" );
			if ( initName != null ) {
				ConsoleWriteLine( "Using Configuration: "+initName+" from "+initAsmb );
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
					ConsoleWriteLine( "# SEVERE # Error instantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": " + ex );//+":\n"+ex.StackTrace	);
					ReportExceptionToEventLog("# SEVERE # Error instantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": ", ex);
					// Continue and do normal initialization.
				} catch ( System.TypeLoadException ex ) {
					ConsoleWriteLine("# SEVERE # Error instantiating \"netlog.logging.config.class\" "+
						"specified initialization class, \""+initName+"\": "+ex );//+":\n"+ex.StackTrace	);
					// Continue and do normal initialization.
					ReportExceptionToEventLog("# SEVERE # Error instantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": ", ex);
				}
			}
			//try {
			//	ConsoleWriteLine( "Check for configuration section: " );
			//	NetLogConfigurationSection sect = (NetLogConfigurationSection)
			//		ConfigurationManager.GetSection("LoggingConfiguration");
			//	ConsoleWriteLine( "Found configuration section: " + sect );
			//	if( sect != null ) {
			//		ConsoleWriteLine( "Using configuration section: " + sect );
			//		ActivateConfigurationSection( sect);
			//		configLoaded = true;
			//		return;
			//	}
			//} catch( Exception ex ) {
			//	ReportExceptionToEventLog( "Error Loading LoggingConfiguration Section: ", ex );
			//}
			ConsoleWriteLine( "Check for logging.properties" );

			String props = null;
			try {
				props = System.Environment.GetEnvironmentVariable( "netlog.logging.config.file" );
				if( props == null )
					props = ConfigurationManager.AppSettings["netlog.logging.config.file"];
				if( props == null )
					props = "logging.properties";
				ConsoleWriteLine( "Using logging.properties: " + props );
				if( fw != null ) {
					ConsoleWriteLine( "Dropping existing watcher for: " + fw.Path );
					fw.EnableRaisingEvents = false;
				}
				ConsoleWriteLine( "Setting up FileSystemWatcher for: " + props );
				fw = new FileSystemWatcher();
				fw.Path = new FileInfo( props ).DirectoryName;
				fw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;// | NotifyFilters.DirectoryName;
				fw.Changed += new FileSystemEventHandler( OnChangedConfig );
				fw.Filter = new FileInfo( props ).Name;
				if( new FileInfo( props ).Exists == false ) {
					ConsoleWriteLine( "The designated properties file: " + props + " does not exist in " +
							Directory.GetCurrentDirectory() );
					return;
				}
				Exception ex = null;
				bool readIt = false;
				ConsoleWriteLine( "Attempting to reading properties from: " + props );
				for( int i = 0; !readIt && i < 3; ++i ) {
					try {
						using( StreamReader sr = new StreamReader( props ) ) {
							ConsoleWriteLine( "Opening " + props + " returned " + sr );
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
				ConsoleWriteLine( "Reading properties from: " + props+" was "+(readIt ? "" : "not ")+"successful" );
				if( !readIt ) {
					configLoaded = false;
					ConsoleWriteLine( "The Logging properties file, \"" + props + "\", could not be read!" );
					ConsoleWriteLine( "# SEVERE # " + ex.Message + ": " + ex.StackTrace );
					ReportExceptionToEventLog( "The Logging properties file, \"" + props + "\", could not be read!", ex );
				} else
                {
                    configLoaded = true;
                }
            } catch( Exception e ) {
				configLoaded = false;
				ReportExceptionToEventLog( "The Logging properties file, \"" + props + "\", could not be read!", e );
				ConsoleWriteLine( "The Logging properties file, \"" + props + "\", could not be read!" );
				ConsoleWriteLine( "# SEVERE # " + e.Message + ": " + e.StackTrace );
			} finally {
				ConsoleWriteLine( "Fw Events now raised: " + (fw != null) );
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
			Dictionary<String, Logger> nlog = new Dictionary<string, Logger>(loggers);
			foreach( LoggerConfiguration lg in sect.Loggers ) {
				string name = lg.Name;
				string handler = lg.Handler;
				Logger log;
				if( lg.ClassName == null || lg.ClassName.Length == 0 || loggers.ContainsKey(name) ) {
					if( nlog.ContainsKey(name) ) {
						throw new InvalidOperationException("Logger with name " + name + " is already defined, can't instantiate as \"" + lg.ClassName + "\"");
					}
					log = Logger.NeedNewLogger(name);
				} else {
					string asmb = lg.AssemblyName;
					log = (Logger)Activator.CreateInstance(asmb.Length == 0 ? null : asmb, lg.ClassName).Unwrap();
				}
				nlog[ name ] = log;
				if( handlers.ContainsKey(handler) ) {
					log.AddHandler(handlers[ handler ]);
				}
				foreach( PropertyConfiguration p in lg.Properties ) {
					SetPropertyOn(log, p);
				}
			}
			loggers = nlog;
		}

		private void SetPropertyOn( object fmtr, PropertyConfiguration p ) {
			putPropValue(fmtr, p.Name, p.Value);
		}

		private static void CreateEventLogSource() {
			try {
				if( EventLog.SourceExists( EventLogParms.Source ) == false ) {
					EventLog.CreateEventSource( EventLogParms.Source, EventLogParms.LogName );
				}
			}
			catch( Exception ex ) {
				if( sw != null ) 
					sw.WriteLine( ex.Message + ":\n" + ExpandStackTraceFor( ex ) );
				EventLog.WriteEntry( EventLogParms.Source, ex.Message + ":\n"+ ExpandStackTraceFor( ex ), EventLogEntryType.Error, 0, -1 );
			}
		}
		public static void ReportExceptionToEventLog( string msg, Exception ex ) {
			string str = "";
			try {
				if( sw != null )
					sw.WriteLine( msg + ": " + ex.Message + ":\n" + ExpandStackTraceFor( ex ) );
				CreateEventLogSource();
				if( ex != null ) {
					str = ExpandStackTraceFor(ex);
				}
				EventLog.WriteEntry(EventLogParms.Source, msg + ( ex == null ? "" : ( ": " + str ) ), EventLogEntryType.Error, 0, -1);
			} catch( Exception exx ) {
				try {
					ConsoleWriteLine( "Error writing to eventLog for exception: " + exx );
					ConsoleWriteLine( msg + ": " + ex.Message + "\n" + ex );
				} catch( Exception exxx ) {
					Console.WriteLine( exxx.Message + "\n" + exxx.StackTrace );
				}
			}
		}

		private static string ExpandStackTraceFor( Exception ex ) {
			StringBuilder b = new StringBuilder();
			StreamFormatter.AddStackTrace( b, "\n\r", ex );
			return b.ToString();
		}


		public void ReadConfiguration( StreamReader rd ) {
			configLoaded = true;
			readStream(rd);
		}

		internal class CachedLevel {
			internal DateTime when;
			internal Level level;
			public CachedLevel( Level l ) {
				UpdateWith( l );
			}
			public void UpdateWith( Level l ) {
				level = l;
				when = DateTime.UtcNow;
			}
			public bool Expired {
				get {
					return DateTime.UtcNow - when > new TimeSpan( 0, 0, 10 );
				}
			}
		}

		private Dictionary<string,CachedLevel> cachedLevels = new Dictionary<string, CachedLevel>();
		/// <summary>
		/// This is an implementation of String.SubString which seems to be faster than the
		/// builtin version.  It's use of the local stack char[] seems to make things faster with
		/// out as much overhead in object creation?  Not really sure, but the built-in was ~3.2% of
		/// CPU time, and this once is running below ~2.8%.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		private String subStringOf( String nm, int start, int end ) {
			char[] arr = new char[ end - start ];
			for( int i = start; i < end; ++i ) {
				arr[ i - start ] = nm[ i ];
			}
			return new string(arr);
		}
		/// <summary>
		/// Called to get the level of a Logger name which does not explicitly exist.  This is typically
		/// going to happen for all loggers without an explicit Level set on the Logger itself.  The Logger.Level
		/// getter will call here to ask for any assigned property values or parent logger Levels set explicitly.
		/// </summary>
		/// <param name="name">The name of the Logger to look for an implicit Level value for</param>
		/// <returns></returns>
		public Level LevelOfLogger( string name ) {
			if( levels.ContainsKey(name) ) {
				return levels[ name ];
			}
			if( cachedLevels.ContainsKey( name ) && cachedLevels[ name ].Expired == false )
				return cachedLevels[ name ].level;

			string nm = name;
			int idx;
			while( (idx = nm.LastIndexOf('.')) >= 0 ) {
				nm = subStringOf(nm, 0, idx );

				// If there is a physical Logger at this level, use any
				// level explicitly set there.
				if( loggers.ContainsKey(nm) && loggers[ nm ].level != null ) {
					Level l = loggers[ nm ].level;
					CachedLevel cl;
					if( cachedLevels.ContainsKey( nm ) ) {
						cl = cachedLevels[ nm ];
						cl.UpdateWith( l );
					}
					else {
						ReplaceCachedDictionaryItem( nm, new CachedLevel( l ) );
					}
					return l;
				}
				// If there is a property set Level at a particular level, use that
				// level.
				if( levels.ContainsKey(nm) ) {
					Level l = levels[ nm ];
					CachedLevel cl;
					if( cachedLevels.ContainsKey( nm ) ) {
						cl = cachedLevels[ nm ];
						cl.UpdateWith( l );
					}
					else {
						ReplaceCachedDictionaryItem( nm, new CachedLevel( l ) );
					}
//					cachedLevels[ nm ] = new CachedLevel( l );
					return l;
				}
			}
			try {
				// The top level logger has a level set for itself.
				// technically the loop above also covers this logger name
				// but we'll do it explicitly here too just to make sure.
				if( loggers.ContainsKey( "" ) ) {
					Level l =  loggers[ "" ].Level;
					ReplaceCachedDictionaryItem("", new CachedLevel(l));
//					cachedLevels[ "" ] = new CachedLevel( l );
					return l;
				}
			} catch( Exception ex ) {
				ReportExceptionToEventLog("Can't get level for '' Logger instance", ex);
			}
			return Level.ALL;
		}

		private void ReplaceCachedDictionaryItem(string nm, CachedLevel cachedLevel)
		{
			Dictionary<String, CachedLevel> lvls;
			Dictionary<String, CachedLevel> old;
			do
			{
				old = cachedLevels;
				lvls = new Dictionary<string, CachedLevel>( old );
				lvls[ nm ] = cachedLevel;
			} while (Interlocked.CompareExchange<Dictionary<String, CachedLevel>>( ref cachedLevels, lvls, old ) != old );
		}
		private int recurseCount = 0;
		private void readStream( StreamReader rd ) {
			try {
				string line;
				if( Interlocked.Increment( ref recurseCount ) > 1 )
					throw new InvalidProgramException( "readStream recursion in NetLog.Logging" );
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
									ConsoleWriteLine( "# ERROR # Error creating handler \""+cls+"\": "+ex.Message+"\n"+ex.Source+": "+ex.StackTrace );
									Exception other = ex.InnerException;
									while( other != null ) {
										ConsoleWriteLine( "InnerException:\n" + other.StackTrace);
										other = other.InnerException;
									}
									continue;
								}
								if( h == null ) {
									ConsoleWriteLine( "Can't load Handler class: "+cls );
									continue;
								}
								handlers[cls] = h;
							} else {
								h = handlers[cls];
							}
							if( log != null && log.IsLoggable(Level.FINE) )
								ConsoleWriteLine( "adding handler: " + cls );
							Logger.GetLogger( "" ).GetHandlers( ).Add( h );
							// rememeber which handlers are in the root logger.
							oh[cls] = h;
						}
						// remove any handlers no longer listed.
						if( log != null && log.IsLoggable(Level.FINE) )
							ConsoleWriteLine( "root handlers: " + Logger.GetLogger( "" ).GetHandlers( ).Count );
						foreach( Handler hh in new List<Handler>( Logger.GetLogger("").GetHandlers() ) ) {
							if( log != null && log.IsLoggable(Level.FINE) )
								ConsoleWriteLine( "Checking if using handler: \"" + hh.GetType( ).FullName + "\": " + oh );
							if ( oh.ContainsKey( hh.GetType( ).FullName ) == false ) {
								Logger.GetLogger("").RemoveHandler( hh );
								if( log != null && log.IsLoggable(Level.FINE) )
									ConsoleWriteLine( "Removing no longer used handler: \"" + hh.GetType( ).FullName + "\"" );
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
						ConsoleWriteLine( "handlers now (" + Logger.GetLogger("").GetHandlers().Count + ") :" + Logger.GetLogger("").GetHandlers()[0]);
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
									ConsoleWriteLine( "Can't set formatter on nonexistent handler: \""+handler+"\"" );
								continue;
							}
							Handler h = handlers[handler];
							ConsoleWriteLine( "set formatter on handler \"" + handler + "\": " + h);
							lock( formatters ) {
								if( formatters.ContainsKey( cls ) == false ) {
									formatters[ cls ] = (Formatter)Activator.CreateInstance( Type.GetType( cls ) );
								} else {
									ConsoleWriteLine( "Already have formatter for class: " + cls );
								}
							}
							Formatter f = formatters[ cls ];
							// set the formatter.
							if( log != null && log.IsLoggable(Level.FINE) )
								ConsoleWriteLine( "setting formatter: " + cls);
							h.Formatter = f;
						}
					} else if( line.StartsWith("config=") ) {
						string[]arr = line.Substring("config=".Length).Split( new char[]{','} ) ;
						foreach( string cls in arr ) {
							ConsoleWriteLine( "instantiating config: " + cls);
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

						ConsoleWriteLine( "set level for \"" + name + "\" to " + l);

						if( handlers.ContainsKey( name ) ) {
							handlers[name].Level = l;
						} else {
							Logger lg = Logger.GetLogger(name);
							lg.Level = l;

							ConsoleWriteLine( "set logger level for \"" + lg.Name + "\" to " + lg.Level);
						}
					} else {
						string[]arr = line.Split(new char[]{ '=' } );
						if( arr.Length == 2 ) {
							string[] nm = arr[0].Split( new char[]{ '.' } );
							string cls = "";
							// Remove the last item.  Could use String.LastIndexOf() instead of loop
							for( int i = 0; i < nm.Length-1; ++i ) {
								if( cls.Length == 0 ) {
									cls = nm[i];
								} else {
									cls = cls + "." + nm[i];
								}
							}
							ConsoleWriteLine( "Found property='" + arr[0] + "' with value =" + arr[1]);
							try {
								if( handlers.ContainsKey( cls ) ) {
									string propnm = nm[ nm.Length-1 ];
									Handler h = handlers[cls];
									ConsoleWriteLine( "Setting handler ("+cls+"="+h+") property '" + propnm + "'=\"" + arr[ 1 ] + "\"" );
									if( h != null ) {
										putPropValue( h, propnm, arr[1] );
									} else {
										ConsoleWriteLine( "# ERROR # Handler property value reference to unused handler class: "+cls);
									}
								} else if( formatters.ContainsKey( cls ) ) {
									string propnm = nm[nm.Length - 1];
									ConsoleWriteLine( "found formatter property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
									Formatter f = formatters[cls];
									putPropValue( f, propnm, arr[1] );
								}
							} catch( Exception ex ) {
								ConsoleWriteLine( "# ERROR # can't set property value: \"" + line + "\": " + ex + "\n" + ex.StackTrace );
								ReportExceptionToEventLog( "Can't set property value: \"" + line + "\"", ex);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				ReportExceptionToEventLog("The Logging configuration stream could not be read", e);
				ConsoleWriteLine( "# SEVERE # "+e.Message+":\n"+e.StackTrace);
			} finally {
				Interlocked.Decrement( ref recurseCount );
			}
		}

		private static readonly Object StreamLock = new Object();

		/// <summary>
		/// If the tmp log file is active, write to it.
		/// </summary>
		/// <param name="val"></param>
		private static void StreamWrite( String val ) {
			if( tmplog.Exists ) {
				lock( StreamLock ) {
					try {
						using( StreamWriter sw = new StreamWriter( tmpfile, true ) ) {
							sw.WriteLine( val );
						}
					}
					catch( Exception ex ) {
						ReportExceptionToEventLog( "Cannot get temp trace file opened: " + tmpfile, ex );
					}
				}
			}
		}

		private static int evid = 0;

		public static void ConsoleWriteLine( string v ) {
			// If not initialized yet, or logging is as low as FINER, then write to EventLog
			if( log == null || log.IsLoggable( Level.FINER ) ) {
				StreamWrite( v );
				CreateEventLogSource();
				Interlocked.Increment( ref evid );
				EventLog.WriteEntry( EventLogParms.Source, v, EventLogEntryType.Information, evid );
			} else if( log != null && log.IsLoggable( Level.FINE ) ) {
				Console.WriteLine( v );
			}
		}

		/// <summary>
		/// Atomically replace the level setting of a level set through properties.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="l"></param>
		private void PutLevel( string name, Level l ) {
			Dictionary<String, Level> nlevels;
			Dictionary<String, Level> cur;
			do
			{
				cur = levels;
				nlevels = new Dictionary<String, Level>( cur );
				nlevels[name] = l;
			} while (Interlocked.CompareExchange<Dictionary<String, Level>>(ref levels, nlevels, cur) != cur);
		}

		protected void putPropValue(object obj, string propnm, string value)
		{
			try {
				Type type = obj.GetType();
				PropertyInfo propInfo = type.GetProperty( propnm );
				if( propInfo == null ) {
					throw new System.ArgumentException( "Can't find property named " + propnm + " for " + obj.GetType().FullName );
				}

				if( propInfo.PropertyType == typeof( Int32 ) ) {
					propInfo.SetValue( obj, int.Parse( value ), null );
				}
				else if( propInfo.PropertyType == typeof( long ) ) {
					propInfo.SetValue( obj, long.Parse( value ), null );
				}
				else if( propInfo.PropertyType == typeof( float ) ) {
					propInfo.SetValue( obj, float.Parse( value ), null );
				}
				else if( propInfo.PropertyType == typeof( bool ) ) {
					propInfo.SetValue( obj, bool.Parse( value ), null );
				}
				else if( propInfo.PropertyType == typeof( string ) ) {
					propInfo.SetValue( obj, value, null );
				}
				else {
					throw new System.ArgumentException( "don't konw how to set property type " + propInfo.PropertyType );
				}
			} catch( Exception ex ) {
				EventLog.WriteEntry( EventLogParms.Source, "Error processing property assignment of " + propnm + " to " + value + " for " + obj+": "+ ex.Message );
				throw;
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
