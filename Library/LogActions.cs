using System;

namespace NetLog.Logging;

public abstract class LogActions : ILogActions {
    protected bool consoleDebug;
    protected Level Level;
    [Obsolete]
    public void finest ( Exception ex ) {
        Finest ( ex );
    }
    public void Finest ( Exception ex ) {
        log( Level.FINEST, ex );
    }
    [Obsolete]
    public void finest ( string msg ) {
        Finest ( msg );
    }
    public void Finest ( string msg ) {
        log( Level.FINEST, msg );
    }
    [Obsolete]
    public void finest ( string msg, Exception ex ) {
        Finest ( msg, ex );
    }
    public void Finest ( string msg, Exception ex ) {
        log( Level.FINEST, msg, ex );
    }

    [Obsolete]
    public void finest ( string msg, Exception ex, params object[ ] parms ) {
        Finest ( msg, ex, parms );
    }
    public void Finest ( string msg, Exception ex, params object[ ] parms ) {
        log( Level.FINEST, msg, ex, parms );
    }

    [Obsolete]
    public void finest ( string msg, params object[] parms ) {
        Finest ( msg, parms );
    }
    public void Finest ( string msg, params object[] parms ) {
        log( Level.FINEST, msg, parms );
    }

    [Obsolete]
    public void finer ( Exception ex ) {
        Finer ( ex );
    }
    public void Finer ( Exception ex ) {
        log( Level.FINER, ex );
    }
    [Obsolete]
    public void finer ( string msg ) {
        Finer ( msg );
    }
    public void Finer ( string msg ) {
        log( Level.FINER, msg );
    }
    [Obsolete]
    public void finer ( string msg, Exception ex ) {
        Finer ( msg, ex );
    }
    public void Finer ( string msg, Exception ex ) {
        log( Level.FINER, msg, ex );
    }

    [Obsolete]
    public void finer ( string msg, Exception ex, params object[ ] parms ) {
        Finer ( msg, ex, parms );
    }
    public void Finer ( string msg, Exception ex, params object[ ] parms ) {
        log( Level.FINER, msg, ex, parms );
    }

    [Obsolete]
    public void finer ( string msg, params object[ ] parms ) {
        Finer ( msg, parms );
    }
    public void Finer ( string msg, params object[ ] parms ) {
        log( Level.FINER, msg, parms );
    }

    [Obsolete]
    public void entering( Type sourceClass, object sourceMethod ) {
        Entering( sourceClass, sourceMethod );
    }
    public void Entering( Type sourceClass, object sourceMethod ) {
        entering(sourceClass.FullName, sourceMethod.ToString());
    }

    [Obsolete]
    public void entering( Type sourceClass, object sourceMethod, params object[] param ) {
        Entering( sourceClass, sourceMethod, param );
    }
    public void Entering( Type sourceClass, object sourceMethod, params object[] param ) {
        entering(sourceClass.FullName, sourceMethod.ToString(), param);
    }

    [Obsolete]
    public void entering( string sourceClass, string sourceMethod ) {
        Entering( sourceClass, sourceMethod );
    }
    public void Entering( string sourceClass, string sourceMethod ) {
        LogRecord rec = new LogRecord(Level.FINER, "ENTRY");
        rec.SourceClassName = sourceClass;
        rec.SourceMethodName = sourceMethod;
        log(rec);
    }

    [Obsolete]
    public void entering(string sourceClass, string sourceMethod, params object[] param) {
        Entering( sourceClass, sourceMethod, param);
    }
    public void Entering(string sourceClass, string sourceMethod, params object[] param) {
        LogRecord rec = new LogRecord(Level.FINER, "ENTRY");
        rec.SourceClassName = sourceClass;
        rec.SourceMethodName = sourceMethod;
        rec.Parameters = param;
        log(rec);
    }
    [Obsolete]
    public void exiting(string sourceClass, string sourceMethod) {
        Exiting( sourceClass, sourceMethod);
    }
    public void Exiting(string sourceClass, string sourceMethod) {
        LogRecord rec = new LogRecord(Level.FINER, "EXIT");
        rec.SourceClassName = sourceClass;
        rec.SourceMethodName = sourceMethod;
        log(rec);
    }

    [Obsolete]
    public void exiting(string sourceClass, string sourceMethod, params object[] param) {
        Exiting( sourceClass, sourceMethod, param);
    }
    public void Exiting(string sourceClass, string sourceMethod, params object[] param) {
        LogRecord rec = new LogRecord(Level.FINER, "EXIT");
        rec.SourceClassName = sourceClass;
        rec.SourceMethodName = sourceMethod;
        rec.Parameters = param;
        log(rec);
    }
    [Obsolete]
    public void throwing(string sourceClass, string sourceMethod, Exception thrown) {
        Throwing( sourceClass, sourceMethod,  thrown);
    }
    public void Throwing(string sourceClass, string sourceMethod, Exception thrown) {
        LogRecord rec = new LogRecord(Level.FINER, "THROW");
        rec.SourceClassName = sourceClass;
        rec.SourceMethodName = sourceMethod;
        rec.Thrown = thrown;
        log(rec);
    }

    [Obsolete]
    public void fine ( Exception ex ) {
        Fine ( ex );
    }
    public void Fine ( Exception ex ) {
        log( Level.FINE, ex );
    }
    [Obsolete]
    public void fine ( string msg ) {
        Fine ( msg );
    }
    public void Fine ( string msg ) {
        log( Level.FINE, msg );
    }
    [Obsolete]
    public void fine( string msg, Exception ex ) {
        Fine( msg, ex );
    }
    public void Fine( string msg, Exception ex ) {
        log(Level.FINE, msg, ex);
    }

    [Obsolete]
    public void fine( string msg, params object[] parms ) {
        Fine( msg, parms );
    }
    public void Fine( string msg, params object[] parms ) {
        log(Level.FINE, msg, parms);
    }
    [Obsolete]
    public void fine( string msg, Exception ex, params object[] parms ) {
        Fine( msg, ex, parms );
    }
    public void Fine( string msg, Exception ex, params object[] parms ) {
        log( Level.FINE, msg, ex, parms );
    }

    [Obsolete]
    public void config ( Exception ex ) {
        Config ( ex );
    }
    public void Config ( Exception ex ) {
        log( Level.CONFIG, ex );
    }
    [Obsolete]
    public void config ( string msg ) {
        Config ( msg );
    }
    public void Config ( string msg ) {
        log( Level.CONFIG, msg );
    }
    [Obsolete]
    public void config( string msg, Exception ex ) {
        Config( msg, ex );
    }
    public void Config( string msg, Exception ex ) {
        log(Level.CONFIG, msg, ex);
    }

    [Obsolete]
    public void config ( string msg, Exception ex, params object[ ] parms ) {
        Config ( msg, ex, parms );
    }
    public void Config ( string msg, Exception ex, params object[ ] parms ) {
        log( Level.CONFIG, msg, ex, parms );
    }

    [Obsolete]
    public void config ( string msg, params object[ ] parms ) {
        Config ( msg, parms );
    }
    public void Config ( string msg, params object[ ] parms ) {
        log( Level.CONFIG, msg, parms );
    }

    [Obsolete]
    public void info ( Exception ex ) {
        Info ( ex );
    }
    public void Info ( Exception ex ) {
        log( Level.INFO, ex );
    }
    [Obsolete]
    public void info ( string msg ) {
        Info ( msg );
    }
    public void Info ( string msg ) {
        log( Level.INFO, msg );
    }
    [Obsolete]
    public void info ( string msg, Exception ex ) {
        Info ( msg, ex );
    }
    public void Info ( string msg, Exception ex ) {
        log( Level.INFO, msg, ex );
    }

    [Obsolete]
    public void info ( string msg, Exception ex, params object[ ] parms ) {
        Info ( msg, ex, parms );
    }
    public void Info ( string msg, Exception ex, params object[ ] parms ) {
        log( Level.INFO, msg, ex, parms );
    }

    [Obsolete]
    public void info ( string msg, params object[ ] parms ) {
        Info ( msg, parms );
    }
    public void Info ( string msg, params object[ ] parms ) {
        log( Level.INFO, msg, parms );
    }

    [Obsolete]
    public void warning ( Exception ex ) {
        Warning ( ex );
    }
    public void Warning ( Exception ex ) {
        log( Level.WARNING, ex );
    }
    [Obsolete]
    public void warning ( string msg ) {
        Warning ( msg );
    }
    public void Warning ( string msg ) {
        log( Level.WARNING, msg );
    }
    [Obsolete]
    public void warning ( string msg, Exception ex ) {
        Warning ( msg, ex );
    }
    public void Warning ( string msg, Exception ex ) {
        log( Level.WARNING, msg, ex );
    }

    [Obsolete]
    public void warning ( string msg, Exception ex, params object[ ] parms ) {
        Warning ( msg, ex, parms );
    }
    public void Warning ( string msg, Exception ex, params object[ ] parms ) {
        log( Level.WARNING, msg, ex, parms );
    }

    [Obsolete]
    public void warning ( string msg, params object[ ] parms ) {
        Warning ( msg, parms );
    }
    public void Warning ( string msg, params object[ ] parms ) {
        log( Level.WARNING, msg, parms );
    }

    [Obsolete]
    public void severe ( Exception ex ) {
        Severe ( ex );
    }
    public void Severe ( Exception ex ) {
        log( Level.SEVERE, ex );
    }
    [Obsolete]
    public void severe ( string msg ) {
        Severe ( msg );
    }
    public void Severe ( string msg ) {
        log( Level.SEVERE, msg );
    }
    [Obsolete]
    public void severe ( string msg, Exception ex ) {
        Severe ( msg, ex );
    }
    public void Severe ( string msg, Exception ex ) {
        log( Level.SEVERE, msg, ex );
    }

    [Obsolete]
    public void severe ( string msg, Exception ex, params object[ ] parms ) {
        Severe ( msg, ex, parms );
    }
    public void Severe ( string msg, Exception ex, params object[ ] parms ) {
        log( Level.SEVERE, msg, ex, parms );
    }

    [Obsolete]
    public void severe ( string msg, params object[ ] parms ) {
        Severe ( msg, parms );
    }
    public void Severe ( string msg, params object[ ] parms ) {
        log( Level.SEVERE, msg, parms );
    }


    [Obsolete]
    public void log ( Level level, Exception ex ) {
        Log ( level, ex );
    }
    public void Log ( Level level, Exception ex ) {
        LogRecord rec = new LogRecord( level, ex.GetType().FullName+": "+ex.Message );
        rec.Thrown = ex;
        log( rec );
    }
    [Obsolete]
    public void log ( Level level, string msg ) {
        Log ( level, msg );
    }
    public void Log ( Level level, string msg ) {
        log( new LogRecord( level, msg ) );
    }
    [Obsolete]
    public void log ( Level level, string msg, Exception ex ) {
        Log ( level, msg, ex );
    }
    public void Log ( Level level, string msg, Exception ex ) {
        LogRecord rec = new LogRecord( level, msg );
        rec.Thrown = ex;
        log( rec );
    }

    [Obsolete]
    public void log ( Level level, string msg, Exception ex, params object[ ] parms ) {
        Log ( level, msg, ex, parms );
    }
    public void Log ( Level level, string msg, Exception ex, params object[ ] parms ) {
        LogRecord rec = new LogRecord( level, msg );
        rec.Parameters = parms;
        rec.Thrown = ex;
        log( rec );
    }

    [Obsolete]
    public void log ( Level level, string msg, params object[ ] parms ) {
        Log ( level, msg, parms );
    }
    public void Log ( Level level, string msg, params object[ ] parms ) {
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

    [Obsolete]
    public void log( LogRecord rec ) {
        Log( rec );
    }
    public abstract void Log( LogRecord rec );
}