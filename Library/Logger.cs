﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace NetLog.Logging
{
	public interface ILogger : ILogActions {
		public Level Level {get;set;}
		public Logger Parent {get;}
		public string Name {get;set;}
		public void AddHandler( Handler h );
		public void RemoveHandler( Handler h ) ;
		public IEnumerable<Handler> GetHandlers();
		public bool AddDetailListener( LogDetailsListener lis );
		public bool RemoveDetailListener( LogDetailsListener lis );
		public void Flush();
		public void ClearHandlers();
	}
	public interface ILogActions {
		public void Finest ( Exception ex );
		public void Finest ( string msg );
		public void Finest ( string msg, Exception ex );
		public void Finest ( string msg, Exception ex, params object[ ] parms );
		public void Finest ( string msg, params object[] parms );
		public void Finer ( Exception ex );
		public void Finer ( string msg );
		public void Finer ( string msg, Exception ex );
		public void Finer ( string msg, Exception ex, params object[ ] parms );
		public void Finer ( string msg, params object[ ] parms );
		public void Entering( Type sourceClass, object sourceMethod );
		public void Entering( Type sourceClass, object sourceMethod, params object[] param );
		public void Entering( string sourceClass, string sourceMethod );
		public void Entering(string sourceClass, string sourceMethod, params object[] param);
		public void Exiting(string sourceClass, string sourceMethod);
		public void Exiting(string sourceClass, string sourceMethod, params object[] param);
		public void Throwing(string sourceClass, string sourceMethod, Exception thrown);
		public void Fine ( Exception ex );
		public void Fine ( string msg );
		public void Fine( string msg, Exception ex );
		public void Fine( string msg, params object[] parms );
		public void Fine( string msg, Exception ex, params object[] parms );
		public void Config ( Exception ex );
		public void Config ( string msg );
		public void Config( string msg, Exception ex );
		public void Config ( string msg, Exception ex, params object[ ] parms );
		public void Config ( string msg, params object[ ] parms );
		public void Info ( Exception ex );
		public void Info ( string msg );
		public void Info ( string msg, Exception ex );
		public void Info ( string msg, Exception ex, params object[ ] parms );
		public void Info ( string msg, params object[ ] parms );
		public void Warning ( Exception ex );
		public void Warning ( string msg );
		public void Warning ( string msg, Exception ex );
		public void Warning ( string msg, Exception ex, params object[ ] parms );
		public void Warning ( string msg, params object[ ] parms );
		public void Severe ( Exception ex );
		public void Severe ( string msg );
		public void Severe ( string msg, Exception ex );
		public void Severe ( string msg, Exception ex, params object[ ] parms );
		public void Severe ( string msg, params object[ ] parms );
		public void Log ( Level level, Exception ex );
		public void Log ( Level level, string msg );
		public void Log ( Level level, string msg, Exception ex );
		public void Log ( Level level, string msg, Exception ex, params object[ ] parms );
		public void Log ( Level level, string msg, params object[ ] parms );
		public bool IsLoggable( Level level );
		public void Log( LogRecord rec );
		[Obsolete]
		public void finest ( Exception ex );
		[Obsolete]
		public void finest ( string msg );
		[Obsolete]
		public void finest ( string msg, Exception ex );
		[Obsolete]
		public void finest ( string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void finest ( string msg, params object[] parms );
		[Obsolete]
		public void finer ( Exception ex );
		[Obsolete]
		public void finer ( string msg );
		[Obsolete]
		public void finer ( string msg, Exception ex );
		[Obsolete]
		public void finer ( string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void finer ( string msg, params object[ ] parms );
		[Obsolete]
		public void entering( Type sourceClass, object sourceMethod );
		[Obsolete]
		public void entering( Type sourceClass, object sourceMethod, params object[] param );
		[Obsolete]
		public void entering( string sourceClass, string sourceMethod );
		[Obsolete]
		public void entering(string sourceClass, string sourceMethod, params object[] param);
		[Obsolete]
		public void exiting(string sourceClass, string sourceMethod);
		[Obsolete]
		public void exiting(string sourceClass, string sourceMethod, params object[] param);
		[Obsolete]
		public void throwing(string sourceClass, string sourceMethod, Exception thrown);
		[Obsolete]
		public void fine ( Exception ex );
		[Obsolete]
		public void fine ( string msg );
		[Obsolete]
		public void fine( string msg, Exception ex );
		[Obsolete]
		public void fine( string msg, params object[] parms );
		[Obsolete]
		public void fine( string msg, Exception ex, params object[] parms );
		[Obsolete]
		public void config ( Exception ex );
		[Obsolete]
		public void config ( string msg );
		[Obsolete]
		public void config( string msg, Exception ex );
		[Obsolete]
		public void config ( string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void config ( string msg, params object[ ] parms );
		[Obsolete]
		public void info ( Exception ex );
		[Obsolete]
		public void info ( string msg );
		[Obsolete]
		public void info ( string msg, Exception ex );
		[Obsolete]
		public void info ( string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void info ( string msg, params object[ ] parms );
		[Obsolete]
		public void warning ( Exception ex );
		[Obsolete]
		public void warning ( string msg );
		[Obsolete]
		public void warning ( string msg, Exception ex );
		[Obsolete]
		public void warning ( string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void warning ( string msg, params object[ ] parms );
		[Obsolete]
		public void severe ( Exception ex );
		[Obsolete]
		public void severe ( string msg );
		[Obsolete]
		public void severe ( string msg, Exception ex );
		[Obsolete]
		public void severe ( string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void severe ( string msg, params object[ ] parms );
		[Obsolete]
		public void log ( Level level, Exception ex );
		[Obsolete]
		public void log ( Level level, string msg );
		[Obsolete]
		public void log ( Level level, string msg, Exception ex );
		[Obsolete]
		public void log ( Level level, string msg, Exception ex, params object[ ] parms );
		[Obsolete]
		public void log ( Level level, string msg, params object[ ] parms );
		[Obsolete]
		public bool isLoggable( Level level );
		[Obsolete]
		public void log( LogRecord rec );
	}
	public delegate void LogDetailsListener(Logger log);
    public class Logger : LogActions, ILogger
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
					if( name.Equals("") ) {
						Handler h = new ConsoleHandler();
						l.handlers.Add( h );
						// This is the only level that we force to .INFO.  All
						// others will find there level here, or explicitly set.
						l.Level = Level.INFO;
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
			get {
				if( name.Equals("") )
					return null;
				string[] arr = this.name.Split(new char[]{ '.' } );
				string ln = "";
				for( int i = 0; i < arr.Count()-1; ++i ) {
					if( ln.Length == 0 ) {
						ln = arr[i];
					} else {
						ln = ln + "." + arr[i];
					}
				}
				return Logger.GetLogger(ln);
			}
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
		public IEnumerable<Handler> GetHandlers() {
			return handlers;
		}

		/// <summary>
		/// WARNING: Only use this constructor for private Logger instances which you wish to not be visible
		/// for remote administration.
		/// </summary>
		/// <param name="name"></param>
		public Logger( string name ) {
			this.name = name;
			this.handlers = new List<Handler>();
		}

		public void ClearHandlers() {
			handlers.Clear();
		}

		public override void Log( LogRecord rec ) {
			rec.LoggerName = name;
			Logger logger = this;
			Level lowest = logger.Level;

			while( logger != null ) {
				if( consoleDebug )
					Console.WriteLine("\"" + logger.Name + "\" handlers: " + logger.handlers.Count);
				if( consoleDebug )
					Console.WriteLine("record level " + rec.Level + ", logger level: " + logger.Level + ", logging");
				if( rec.Level.IntValue < lowest.IntValue || lowest == Level.OFF ) {
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