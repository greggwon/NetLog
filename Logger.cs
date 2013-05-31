using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace NetLog.Logging
{
    public class Logger
    {
		private String name;
		private List<Handler> handlers;
		private Level level;
		private static bool consoleDebug;
		private bool useParentHandlers = true;

		public bool UseParentHandlers {
			get{ return useParentHandlers; }
			set { useParentHandlers = value; }
		}

		public static bool ConsoleDebug
		{
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		public static Logger GetLogger ( object obj ) {
			return GetLogger( obj.GetType().FullName );
		}

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
					l.handlers = new List<Handler>();
					if( name.Equals("") ) {
						Handler h = new ConsoleHandler();
						l.handlers.Add( h );
						if( h.Formatter == null )
							h.Formatter = new StreamFormatter();
					}
					l.level = Level.INFO;
					lm.AddLogger( l );
				} else {
					l = LogManager.Loggers[name];
				}
			}
			return l;
		}

		// Will flush all Handlers and return.
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
				this.level = value;
				if ( consoleDebug )
					Console.WriteLine( Name + " level now " + value );
			}
			get { 
				Level l = this.level;
#if false
				Logger lg = this;
				while ( l == null && lg != null ) {
					if ( consoleDebug )
						Console.WriteLine( "logger: " + lg.Name + ", level: " + ( l == null ? "null" : l.ToString( ) ) );
					lg = lg.Parent;
					if ( lg != null )
						l = lg.level;
				}
				if ( consoleDebug )
					Console.WriteLine( "logger: " + lg.Name + ", level: " + ( l == null ? "null" : l.ToString( ) ) ); 
#endif
				return l == null ? Level.ALL : l;
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
		}

		public void RemoveHandler( Handler h ) {
			handlers.Remove( h );
		}

		public List<Handler> GetHandlers() {
			return handlers;
		}

		private Logger( string name ) {
			this.name = name;
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
		public void finest ( string msg, Exception ex, object param ) {
			log( Level.FINEST, msg, ex, param );
		}
		public void finest ( string msg, Exception ex, object[ ] parms ) {
			log( Level.FINEST, msg, ex, parms );
		}
		public void finest ( string msg, object param ) {
			log( Level.FINEST, msg, param );
		}
		public void finest ( string msg, object[] parms ) {
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
		public void finer ( string msg, Exception ex, object param ) {
			log( Level.FINER, msg, ex, param );
		}
		public void finer ( string msg, Exception ex, object[ ] parms ) {
			log( Level.FINER, msg, ex, parms );
		}
		public void finer ( string msg, object param ) {
			log( Level.FINER, msg, param );
		}
		public void finer ( string msg, object[ ] parms ) {
			log( Level.FINER, msg, parms );
		}

		public void entering(string sourceClass, string sourceMethod)
		{
			LogRecord rec = new LogRecord(Level.FINER, "ENTRY");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			log(rec);
		}
		public void entering(string sourceClass, string sourceMethod, object param)
		{
			LogRecord rec = new LogRecord(Level.FINER, "ENTRY");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			rec.Parameters = new object[] { param };
			log(rec);
		}
		public void entering(string sourceClass, string sourceMethod, object[] param)
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
		public void exiting(string sourceClass, string sourceMethod, object param)
		{
			LogRecord rec = new LogRecord(Level.FINER, "EXIT");
			rec.SourceClassName = sourceClass;
			rec.SourceMethodName = sourceMethod;
			rec.Parameters = new object[] { param };
			log(rec);
		}
		public void exiting(string sourceClass, string sourceMethod, object[] param)
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
		public void fine ( string msg, Exception ex ) {
			log( Level.FINE, msg, ex );
		}
		public void fine ( string msg, Exception ex, object param ) {
			log( Level.FINE, msg, ex, param );
		}
		public void fine ( string msg, Exception ex, object[ ] parms ) {
			log( Level.FINE, msg, ex, parms );
		}
		public void fine ( string msg, object param ) {
			log( Level.FINE, msg, param );
		}
		public void fine ( string msg, object[ ] parms ) {
			log( Level.FINE, msg, parms );
		}

		public void config ( Exception ex ) {
			log( Level.CONFIG, ex );
		}
		public void config ( string msg ) {
			log( Level.CONFIG, msg );
		}
		public void config ( string msg, Exception ex ) {
			log( Level.CONFIG, msg, ex );
		}
		public void config ( string msg, Exception ex, object param ) {
			log( Level.CONFIG, msg, ex, param );
		}
		public void config ( string msg, Exception ex, object[ ] parms ) {
			log( Level.CONFIG, msg, ex, parms );
		}
		public void config ( string msg, object param ) {
			log( Level.CONFIG, msg, param );
		}
		public void config ( string msg, object[ ] parms ) {
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
		public void info ( string msg, Exception ex, object param ) {
			log( Level.INFO, msg, ex, param );
		}
		public void info ( string msg, Exception ex, object[ ] parms ) {
			log( Level.INFO, msg, ex, parms );
		}
		public void info ( string msg, object param ) {
			log( Level.INFO, msg, param );
		}
		public void info ( string msg, object[ ] parms ) {
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
		public void warning ( string msg, Exception ex, object param ) {
			log( Level.WARNING, msg, ex, param );
		}
		public void warning ( string msg, Exception ex, object[ ] parms ) {
			log( Level.WARNING, msg, ex, parms );
		}
		public void warning ( string msg, object param ) {
			log( Level.WARNING, msg, param );
		}
		public void warning ( string msg, object[ ] parms ) {
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
		public void severe ( string msg, Exception ex, object param ) {
			log( Level.SEVERE, msg, ex, param );
		}
		public void severe ( string msg, Exception ex, object[ ] parms ) {
			log( Level.SEVERE, msg, ex, parms );
		}
		public void severe ( string msg, object param ) {
			log( Level.SEVERE, msg, param );
		}
		public void severe ( string msg, object[ ] parms ) {
			log( Level.SEVERE, msg, parms );
		}


		public void log ( Level level, Exception ex ) {
			LogRecord rec = new LogRecord( level, ex.Message );
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
		public void log ( Level level, string msg, object param ) {
			LogRecord rec = new LogRecord( level, msg );
			rec.Parameters = new object[] { param };
			log( rec );
		}

		public void log ( Level level, string msg, Exception ex, object[ ] parms ) {
			LogRecord rec = new LogRecord( level, ex.ToString( ) );
			rec.Parameters = parms;
			rec.Thrown = ex;
			log( rec );
		}

		public void log ( Level level, string msg, Exception ex, object parm ) {
			LogRecord rec = new LogRecord( level, ex.ToString( ) );
			rec.Parameters = new object[]{ parm };
			rec.Thrown = ex;
			log( rec );
		}

		public void log ( Level level, string msg, object[ ] parms )
		{
			LogRecord rec = new LogRecord( level, msg );
			rec.Parameters = parms;
			log (rec) ;
		}
		
		public bool isLoggable( Level level ) {
			return (level.IntValue >= this.Level.IntValue && this.Level != Level.OFF);
		}
		
		public void log( LogRecord rec )
		{
			// stop now if not loggable
			if (rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF)
			{
				return;
			}
			if ( consoleDebug )
				Console.WriteLine( "record level " + rec.Level + ", logger level: " + this.Level + ", logging" );
			rec.LoggerName = name;
			Logger logger = this;
			while( logger != null ) {
				if (consoleDebug)
					Console.WriteLine("\"" + Name + "\" handlers: " + handlers.Count);
				foreach (Handler h in new List<Handler>( logger.GetHandlers( ) ) ) {
					h.Publish( rec );
				}
				if( UseParentHandlers == false )
					break;
				logger = logger.Parent;
			}
		}
	}
}
