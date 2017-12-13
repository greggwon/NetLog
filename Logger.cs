using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Configuration;

namespace NetLog.Logging
{
	public delegate void LogDetailsListener(Logger log);
    public class Logger
    {
		private String name;
		private List<Handler> handlers;
		internal volatile Level level;
		private static bool consoleDebug;
		private volatile bool useParentHandlers = true;
		private List<LogDetailsListener>listeners = new List<LogDetailsListener>();

		public bool UseParentHandlers {
			get{ return useParentHandlers; }
			set { useParentHandlers = value; }
		}

		/// <summary>
		/// Add a detail listener.  Such listeners are called when the log level
		/// changes for this instance, or a handler is added or removed.  This
		/// allows some extra behavior of the logger to be controlled.  For interaction
		/// with other logging/event services, the level change notification can be
		/// used to update some external data values so that logging levels can
		/// be adjusted appropriately there as well.
		/// </summary>
		/// <param name="lis"></param>
		/// <returns>true if added, false if already a listener</returns>
		public bool AddDetailListener( LogDetailsListener lis ) {
			if( listeners.Contains(lis) == false ) {
				listeners.Add(lis);
				NotifyDetailListener(lis);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Remove a detail listener
		/// </summary>
		/// <param name="lis"></param>
		/// <returns>true if removed, false if not a registered listener</returns>
		public bool RemoveDetailListener( LogDetailsListener lis ) {
			if( listeners.Contains(lis) ) {
				listeners.Remove(lis);
				return true;
			}
			return false;
		}
		public static bool ConsoleDebug
		{
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		public static Logger GetLogger ( object obj ) {
			return GetLogger( obj.GetType().FullName );
		}

		internal static Logger NeedNewLogger( string name) {
			Logger l;
			// Have to do this up front because it may create logging instances
			// to preset levels when logging.properties has such content, and so
			// we want to find those logger instances, instead of creating them here,
			// and then not seeing those instances if we do things out of order.
			LogManager lm = LogManager.GetLogManager( );
			if ( consoleDebug )
				Console.WriteLine( "Get logger \"" + name + "\": have? " + ( LogManager.Loggers.ContainsKey( name ) ? ( "YES, Level: " + LogManager.Loggers[name].Level ) : "NO" ) );
			
			// lock to make sure we only insert one instance
			lock( LogManager.Loggers ) {
				if( LogManager.Loggers.ContainsKey( name ) == false ) {
					l = new Logger( name );
				} else {
					l = LogManager.Loggers[name];
				}
			}
			return l;
		}

		/// <summary>
		/// Get the logger instance associated with the passed name.  Typically name will
		/// be passed from a class level field initialization something like:
		/// 
		///		private static Logger log = Logger.GetLogger(typeof(ThisClassesName).FullName);
		///		
		/// The use of FullName will allow the software to create a hierarchial name
		/// space that can be easily controlled to turn various classes and subclasses and
		/// innerclasses logging on and off as needed.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Logger GetLogger( String name ) {
			Logger l;
            Level appLevel = GetAppLogLevel();
            // Have to do this up front because it may create logging instances
            // to preset levels when logging.properties has such content, and so
            // we want to find those logger instances, instead of creating them here,
            // and then not seeing those instances if we do things out of order.
            LogManager lm = LogManager.GetLogManager( );
			if ( consoleDebug )
				Console.WriteLine( "Get logger \"" + name + "\": have? " + ( LogManager.Loggers.ContainsKey( name ) ? ( "YES, Level: " + LogManager.Loggers[name].Level ) : "NO" ) );
			lock( LogManager.Loggers ) {
				if( LogManager.Loggers.ContainsKey( name ) == false ) {
					l = new Logger( name );
				    l.Level = appLevel;
					if( name.Equals("") ) {
						Handler h = lm.GetDefaultHandler();
						l.handlers.Add( h );
						// This is the only level that we force to .INFO.  All
						// others will find there level here, or explicitly set.
						l.Level = appLevel;
						if( h.Formatter == null )
							h.Formatter = new StreamFormatter();
					}
					lm.AddLogger( l );
				} else {
					l = LogManager.Loggers[name];
				}
			}
			return l;
		}

        private static Level GetAppLogLevel()
        {
            Level level = Level.SEVERE; // default value

            string appLoggingLevel = ConfigurationManager.AppSettings["appLoggingLevel"];

            if (null != appLoggingLevel)
            {
                level = Level.parse(appLoggingLevel);

                if (level == Level.ALL && appLoggingLevel.ToUpper().Trim() != "ALL")
                {
                    level = Level.SEVERE;
                }
            }
            return level;
        }

        /// <summary>
        /// Flush all handlers associated with this logger
        /// </summary>
        public void Flush() {
			Logger logger = this;
			while ( logger != null ) {
				if ( consoleDebug )
					Console.WriteLine( "Flushing \"" + Name + "\" handlers: " + handlers.Count );
				foreach ( Handler h in logger.GetHandlers( ) ) {
					h.Flush();
				}
				if ( UseParentHandlers == false )
					break;
				logger = logger.Parent;
			}
		}

		public Level Level {
			set {
				if( value.IntValue == Level.ALL.IntValue ) {
					Console.WriteLine("All Level Set For logger=" + name);
				}
				this.level = value;
				if ( consoleDebug )
					Console.WriteLine( Name + " level now " + value );
				NotifyDetailListeners();
			}
			get { 
				Level l = this.level;
				return l == null ? 
					(LogManager.GetLogManager().LevelOfLogger(name)) :
					l;
			}
		}

		private void NotifyDetailListeners() {
			foreach( LogDetailsListener l in listeners ) {
				NotifyDetailListener(l);
			}
		}
		private void NotifyDetailListener( LogDetailsListener l ) {
			try {
				l(this);
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog(string.Format(
					"Error in LogDetailsListener: {0}: {1}", l, ex.Message), ex);
			}
		}

		public Logger Parent {
			get; internal set;
		}

		public string Name {
			set { this.name = value; }
			get { return this.name; }
		}

		public void AddHandler( Handler h ) {
			handlers.Add(h );
			NotifyDetailListeners();
		}

		public void RemoveHandler( Handler h ) {
			handlers.Remove( h );
			try {
				h.Close();
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog( ex.Message, ex );
			}
			NotifyDetailListeners();
		}

		public List<Handler> GetHandlers() {
			return handlers;
		}

		/// <summary>
		/// WARNING: Only use this constructor for private Logger instances which you wish to not be visible
		/// for remote administration.
		/// </summary>
		/// <param name="name"></param>
		public Logger( string name ) {
			this.name = name;
			if( name.Equals( "" ) ) {
				Parent = null;
			}
			else {
				int idx = this.name.LastIndexOf( '.' );
				if( idx == -1 ) {
					Parent = Logger.GetLogger( "" );
				}
				else {
					string parName = name.Substring( 0, idx );
					Parent = Logger.GetLogger( parName );
				}
			}
			this.handlers = new List<Handler>();
		}

		public void finest ( Exception ex ) {
			log( Level.FINEST, ex );
		}
		public void finest ( string msg ) {
			log( Level.FINEST, msg );
		}
		public void finest ( string msg, Exception ex ) {
			log( Level.FINEST, msg, ex );
		}

		public void finest ( string msg, Exception ex, params object[ ] parms ) {
			log( Level.FINEST, msg, ex, parms );
		}

		public void finest ( string msg, params object[] parms ) {
			log( Level.FINEST, msg, parms );
		}

		public void finer ( Exception ex ) {
			log( Level.FINER, ex );
		}
		public void finer ( string msg ) {
			log( Level.FINER, msg );
		}
		public void finer ( string msg, Exception ex ) {
			log( Level.FINER, msg, ex );
		}

		public void finer ( string msg, Exception ex, params object[ ] parms ) {
			log( Level.FINER, msg, ex, parms );
		}

		public void finer ( string msg, params object[ ] parms ) {
			log( Level.FINER, msg, parms );
		}

		public void entering( Type sourceClass, object sourceMethod ) {
			entering(sourceClass.FullName, sourceMethod.ToString());
		}

		public void entering( Type sourceClass, object sourceMethod, params object[] param ) {
			entering(sourceClass.FullName, sourceMethod.ToString(), param);
		}

		public void entering( string sourceClass, string sourceMethod ) {
			LogRecord rec = new LogRecord(Level.FINER, "ENTRY");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			log(rec);
		}

		public void entering(string sourceClass, string sourceMethod, params object[] param)
		{
			LogRecord rec = new LogRecord(Level.FINER, "ENTRY");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			rec.Parameters = param;
			log(rec);
		}
		public void exiting(string sourceClass, string sourceMethod)
		{
			LogRecord rec = new LogRecord(Level.FINER, "EXIT");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			log(rec);
		}

		public void exiting(string sourceClass, string sourceMethod, params object[] param)
		{
			LogRecord rec = new LogRecord(Level.FINER, "EXIT");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			rec.Parameters = param;
			log(rec);
		}
		public void throwing(string sourceClass, string sourceMethod, Exception thrown)
		{
			LogRecord rec = new LogRecord(Level.FINER, "THROW");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			rec.Thrown = thrown;
			log(rec);
		}

		public void fine ( Exception ex ) {
			log( Level.FINE, ex );
		}
		public void fine ( string msg ) {
			log( Level.FINE, msg );
		}
		public void fine( string msg, Exception ex ) {
			log(Level.FINE, msg, ex);
		}

		public void fine( string msg, params object[] parms ) {
			log(Level.FINE, msg, parms);
		}
		public void fine( string msg, Exception ex, params object[] parms ) {
			log( Level.FINE, msg, ex, parms );
		}

		public void config ( Exception ex ) {
			log( Level.CONFIG, ex );
		}
		public void config ( string msg ) {
			log( Level.CONFIG, msg );
		}
		public void config( string msg, Exception ex ) {
			log(Level.CONFIG, msg, ex);
		}

		public void config ( string msg, Exception ex, params object[ ] parms ) {
			log( Level.CONFIG, msg, ex, parms );
		}

		public void config ( string msg, params object[ ] parms ) {
			log( Level.CONFIG, msg, parms );
		}

		public void info ( Exception ex ) {
			log( Level.INFO, ex );
		}
		public void info ( string msg ) {
			log( Level.INFO, msg );
		}
		public void info ( string msg, Exception ex ) {
			log( Level.INFO, msg, ex );
		}

		public void info ( string msg, Exception ex, params object[ ] parms ) {
			log( Level.INFO, msg, ex, parms );
		}

		public void info ( string msg, params object[ ] parms ) {
			log( Level.INFO, msg, parms );
		}

		public void warning ( Exception ex ) {
			log( Level.WARNING, ex );
		}
		public void warning ( string msg ) {
			log( Level.WARNING, msg );
		}
		public void warning ( string msg, Exception ex ) {
			log( Level.WARNING, msg, ex );
		}

		public void warning ( string msg, Exception ex, params object[ ] parms ) {
			log( Level.WARNING, msg, ex, parms );
		}

		public void warning ( string msg, params object[ ] parms ) {
			log( Level.WARNING, msg, parms );
		}

		public void severe ( Exception ex ) {
			log( Level.SEVERE, ex );
		}
		public void severe ( string msg ) {
			log( Level.SEVERE, msg );
		}
		public void severe ( string msg, Exception ex ) {
			log( Level.SEVERE, msg, ex );
		}

		public void severe ( string msg, Exception ex, params object[ ] parms ) {
			log( Level.SEVERE, msg, ex, parms );
		}

		public void severe ( string msg, params object[ ] parms ) {
			log( Level.SEVERE, msg, parms );
		}


		public void log ( Level level, Exception ex ) {
			LogRecord rec = new LogRecord( level, ex.GetType().FullName+": "+ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void log ( Level level, string msg )
		{
			log( new LogRecord( level, msg ) );
		}
		public void log ( Level level, string msg, Exception ex ) {
			LogRecord rec = new LogRecord( level, msg );
			rec.Thrown = ex;
			log( rec );
		}

		public void log ( Level level, string msg, Exception ex, params object[ ] parms ) {
			LogRecord rec = new LogRecord( level, msg );
			rec.Parameters = parms;
			rec.Thrown = ex;
			log( rec );
		}

		public void log ( Level level, string msg, params object[ ] parms )
		{
			LogRecord rec = new LogRecord( level, msg );
			rec.Parameters = parms;
			log (rec) ;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		[Obsolete]
		public bool isLoggable( Level level ) {
			return IsLoggable(level);
		}

		public bool IsLoggable( Level level ) {
			return ( level.IntValue >= this.Level.IntValue && this.Level != Level.OFF );
		}

		public void log( LogRecord rec )
		{
			rec.LoggerName = name;
			Logger logger = this;
			Level lowest = logger.Level;

			while( logger != null ) {
				if( consoleDebug )
					Console.WriteLine("\"" + logger.Name + "\" handlers: " + logger.handlers.Count);
				if( consoleDebug )
					Console.WriteLine("record level " + rec.Level + ", logger level: " + logger.Level + ", logging");
				//if( rec.Level == null ) {
				//	throw new NullReferenceException( "LogRecord is null, can not log: " + logger.handlers[ 0 ].Formatter.format( rec ) );
				//}
				if( rec.Level != null && ( rec.Level.IntValue < lowest.IntValue || lowest == Level.OFF ) ) {
					return;
				}
				//if( lowest.IntValue > logger.Level.IntValue ) {
				//	lowest = logger.Level;
				//}
				if( logger.GetHandlers() != null ) {
					foreach( Handler h in new List<Handler>(logger.GetHandlers()) ) {
						h.Publish(rec);
					}
				}
				if( logger.UseParentHandlers == false )
					break;
				logger = logger.Parent;
			}
		}
	}
}
