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
		private static bool primordialInit;
		private static FileSystemWatcher fw;

		private static Boolean configLoaded;
		private bool consoleDebug;

		static LogManager() {
			string instName = System.Environment.GetEnvironmentVariable("netlog.logging.logmanager");
			if( instName != null ) {
				mgr = (LogManager)Activator.CreateInstance( Type.GetType( instName ) );
			} else {
				mgr = new LogManager();
			}
		}

		public bool ConsoleDebug {
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		internal static Dictionary<string,Logger> Loggers {
			get { return loggers; }
		}

		private void OnChangedConfig ( object source, FileSystemEventArgs args ) {
			if ( consoleDebug )
				Console.WriteLine( "logging.properties file changed: " + args.FullPath );
			lock ( loggers ) {
				mgr.ReadConfiguration( );
			}			
		}

		public void ReadConfiguration() {
			string initName = System.Environment.GetEnvironmentVariable( "netlog.logging.config.class" );
			string initAsmb = System.Environment.GetEnvironmentVariable( "netlog.logging.config.assembly" );
			if ( initName != null ) {
				if( primordialInit )
					return;
				primordialInit = true;
				// Create an instance to allow initialization to run from its constructors actions.
				try {
					int idx = initName.LastIndexOf(".");
					string assemb = initAsmb;
					if( initAsmb == null )
						assemb = initName.Substring(0,idx);
					string name = initName.Substring( idx+1 );
//					if( Type.GetType( initName ) != null ) {
						Activator.CreateInstance( assemb, initName );
						return;
//					} else {
//						Console.WriteLine( "# SEVERE # Error loading config initialization class, \""+initName+"\"" );
//					}
				} catch ( System.IO.FileNotFoundException ex ) {
					Console.WriteLine( "# SEVERE # Error insantiating \"netlog.logging.config.class\" " +
						"specified initialization class, \"" + initName + "\": " + ex );//+":\n"+ex.StackTrace	);
					// Continue and do normal initialization.
				} catch ( System.TypeLoadException ex ) {
					Console.WriteLine("# SEVERE # Error insantiating \"netlog.logging.config.class\" "+
						"specified initialization class, \""+initName+"\": "+ex );//+":\n"+ex.StackTrace	);
					// Continue and do normal initialization.
				}
			}
			String props = "N/A";
			try
			{
				props = System.Environment.GetEnvironmentVariable("netlog.logging.config.file");
				if( props == null )
					props = "logging.properties";
				if( fw != null ) {
					if ( consoleDebug )
						Console.WriteLine( "Dropping existing watcher for: " + fw.Path );
					fw.EnableRaisingEvents = false;
				}
				fw = new FileSystemWatcher();
				fw.Path = new FileInfo(props).DirectoryName;
				fw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
				fw.Changed += new FileSystemEventHandler(OnChangedConfig);
				fw.Filter = new FileInfo( props ).Name;
				fw.EnableRaisingEvents = true;
				if( new FileInfo( props ).Exists == false ) {
					if( consoleDebug ) {
						Console.WriteLine("The designated properties file: "+props+" does not exist" );
					}
					return;
				}
				Exception ex = null;
				bool readIt = false;
				for( int i = 0; i < 3; ++i ) {
					try {
						StreamReader sr = new StreamReader( props );
						try {
							configLoaded = true;
							readStream(sr);
							readIt = true;
						} finally {
							sr.Close();
						}
					} catch (Exception e) {
						configLoaded = false;
						if( ex == null )
							ex = e;
					}
				}
				if( !readIt ) {
					configLoaded = false;
					Console.WriteLine( "The Logging properties file, \"" + props + "\", could not be read!" );
					Console.WriteLine( "# SEVERE # " + ex.Message + ": " + ex.StackTrace );
				}
			} catch (Exception e) {
				configLoaded = false;
				Console.WriteLine("The Logging properties file, \""+props+"\", could not be read!");
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
						Dictionary<string, Handler> oh = new Dictionary<string, Handler>();
						string[]arr = line.Substring( "handlers=".Length ).Split( new char[]{','} ) ;
						if( arr.Count() > 0 )
							Logger.getLogger("").GetHandlers().Clear();
						foreach( string cls in arr ) {
							Handler h;
							if( handlers.ContainsKey( cls ) == false ) {
								try {
									h = (Handler)Activator.CreateInstance( Type.GetType(cls) );
								} catch( Exception ex ) {
									Console.WriteLine( "# ERROR # Error creating handler \""+cls+"\": "+ex.Message+"\n"+ex.Source+": "+ex.StackTrace );
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
							if ( consoleDebug )
								Console.WriteLine( "adding handler: " + cls );
							Logger.getLogger( "" ).GetHandlers( ).Add( h );
							// rememeber which handlers are in the root logger.
							oh[cls] = h;
						}
						// remove any handlers no longer listed.
						if ( consoleDebug )
							Console.WriteLine( "root handlers: " + Logger.getLogger( "" ).GetHandlers( ).Count );
						foreach( Handler hh in new List<Handler>( Logger.getLogger("").GetHandlers() ) ) {
							if ( consoleDebug )
								Console.WriteLine( "Checking if using handler: \"" + hh.GetType( ).FullName + "\": " + oh );
							if ( oh.ContainsKey( hh.GetType( ).FullName ) == false ) {
								Logger.getLogger("").RemoveHandler( hh );
								if ( consoleDebug )
									Console.WriteLine( "Removing no longer used handler: \"" + hh.GetType( ).FullName + "\"" );
							}
						}

						// makes sure that at least a console handler is active if nothing else.
						if( Logger.getLogger("").GetHandlers().Count == 0 ) {
							// put back a console handler if the handlers could not be loaded
							Handler h = new ConsoleHandler();
							if ( handlers.ContainsKey( h.GetType( ).FullName ) == false ) {
								handlers[h.GetType( ).FullName] = h;
							}
							Logger.getLogger("").AddHandler( h );
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
					} else if ( line.Contains( ".level=" ) || line.Contains( ".Level=" ) ) {
						String name;
						int lidx;
						if( line.Contains( ".level=" ) )
							name = line.Substring( 0, lidx= line.IndexOf(".level=") );
						else // if( line.Contains( ".Level=" ) )
							name = line.Substring( 0, lidx=line.IndexOf(".Level=") );

						String level = line.Substring( lidx+(".level=".Length) );
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
							try {
								if( handlers.ContainsKey( cls ) ) {
									string propnm = nm[ nm.Length-1 ];
									if (consoleDebug)
										Console.WriteLine("found handler property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
									Handler h = handlers[cls];
									if( h != null ) {
										putPropValue( h, propnm, arr[1] );
									} else {
										Console.WriteLine("# ERROR # Handler property value reference to unused handler class: "+cls);
									}
								} else if( formatters.ContainsKey( cls ) ) {
									string propnm = nm[nm.Length - 1];
									if (consoleDebug)
										Console.WriteLine("found handler property \"" + cls + "\", prop=" + propnm + " to \"" + arr[1] + "\"");
									Formatter f = formatters[cls];
									putPropValue( f, propnm, arr[1] );
								}
							} catch( Exception ex ) {
								Console.WriteLine( "# ERROR # can't set property value: \""+line+"\": "+ex+"\n"+ex.StackTrace );
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

		protected void putPropValue(object obj, string propnm, string value)
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
	}
}
