using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Seqtech.Logging
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

		public static Logger getLogger( String name ) {
			Logger l;
			if ( consoleDebug )
				Console.WriteLine("Get logger \"" + name + "\"");
			lock( LogManager.Loggers ) {
				if( LogManager.Loggers.ContainsKey( name ) == false ) {
					l = new Logger( name );
					l.handlers = new List<Handler>();
					if( name.Equals("") ) {
						Handler h = new ConsoleHandler();
						l.handlers.Add( h );
						if( h.Formatter == null )
							h.Formatter = new StreamFormatter( false, false, false );
						l.level = Level.INFO;
					}
					LogManager.GetLogManager().AddLogger(l);
				} else {
					l = LogManager.Loggers[name];
				}
			}
			return l;
		}

		public Level Level {
			set { this.level = value; }
			get { 
				Level l = this.level;
				Logger lg = this;
				while( l == null && lg != null ) {
					if (consoleDebug)
						Console.WriteLine("logger: " + lg.Name + ", level: " + (l == null ? "null" : l.ToString()));
					lg = lg.Parent;
					if( lg != null )
						l = lg.level;
				}
				if (consoleDebug)
					Console.WriteLine("logger: " + lg.Name + ", level: " + (l == null ? "null" : l.ToString()));
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
				return Logger.getLogger(ln);
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
			LogRecord rec = new LogRecord( Level.FINEST, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void finest ( string msg )
		{
			LogRecord rec = new LogRecord(Level.FINEST, msg);
			log(rec);
		}
		public void finer ( Exception ex ) {
			LogRecord rec = new LogRecord( Level.FINER, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void finer ( string msg )
		{
			LogRecord rec = new LogRecord(Level.FINER, msg);
			log(rec);
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

		public void fine(string msg)
		{
			LogRecord rec = new LogRecord( Level.FINE, msg );
			log(rec);
		}
		public void fine ( Exception ex ) {
			LogRecord rec = new LogRecord( Level.FINE, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void config ( string msg ) {
			LogRecord rec = new LogRecord( Level.CONFIG, msg );
			log( rec );
		}
		public void config ( Exception ex ) {
			LogRecord rec = new LogRecord( Level.INFO, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void info ( string msg )
		{
			LogRecord rec = new LogRecord( Level.INFO, msg );
			log(rec);
		}
		public void info ( Exception ex ) {
			LogRecord rec = new LogRecord( Level.INFO, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void warning ( Exception ex ) {
			LogRecord rec = new LogRecord( Level.WARNING, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void warning ( string msg )
		{
			LogRecord rec = new LogRecord(Level.WARNING, msg);
			log(rec);
		}
		public void severe ( string msg ) {
			LogRecord rec = new LogRecord( Level.SEVERE, msg );
			log( rec );
		}
		public void severe ( Exception ex ) {
			LogRecord rec = new LogRecord( Level.SEVERE, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void log ( Level level, string msg )
		{
			log( new LogRecord( level, msg ) );
		}
		public void log( Level level, string msg, Exception ex )
		{
			LogRecord rec = new LogRecord( level, msg );
			rec.Thrown = ex;
			log( rec );
		}
		public void log( Level level, Exception ex )
		{
			LogRecord rec = new LogRecord( level, ex.Message );
			rec.Thrown = ex;
			log( rec );
		}
		public void log(Level level, string msg, Exception ex, object[] parms)
		{
			LogRecord rec = new LogRecord(level, ex.ToString());
			rec.Parameters = parms;
			rec.Thrown = ex;
			log (rec) ;
		}
		public void log(Level level, string msg, object[] parms)
		{
			LogRecord rec = new LogRecord( level, msg );
			rec.Parameters = parms;
			log (rec) ;
		}
					
		public void log( LogRecord rec )
		{
			// stop now if not loggable
			if (rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF)
			{
				return;
			}
			rec.LoggerName = name;
			Logger logger = this;
			while( logger != null ) {
				if (consoleDebug)
					Console.WriteLine("\"" + Name + "\" handlers: " + handlers.Count);
				foreach (Handler h in logger.GetHandlers())
				{
					h.Publish( rec );
				}
				if( UseParentHandlers == false )
					break;
				logger = logger.Parent;
			}
		}
	}
}
