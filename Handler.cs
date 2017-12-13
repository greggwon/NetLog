using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NetLog.Logging
{

	public abstract class Handler
	{
		private ErrorManager errorMgr;
		private Formatter fmtr;
		private Filter filter;
		private Level level;
		protected long seq = 1;
		private bool limiting = false, holding =false;
		private bool haveSuffix, havePrefix;
		private string suffix, prefix;
		private int waitCount = 0, holdCount = 0;
		private ConcurrentQueue<LogRecord> records = new ConcurrentQueue<LogRecord>( );
		private bool consoleDebug;

		public bool ConsoleDebug {
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		public int WaitCount {
			get { return waitCount; }
			set { waitCount = value; limiting = ( value > 0 ); }
		}

		public int HoldCount {
			get { return holdCount; }
			set { holdCount = value; holding = ( value > 0 ); }
		}

		public Handler() {
			level = Level.ALL;
			Formatter = new StreamFormatter(false, true, false);
			// Only hold a thread for 20 logging records before letting
			// it continue and causing a following logging call to use
			// that thread.
			WaitCount = 20;
		}

		protected long NextSequence {
			get { lock(this) { return seq++; } }
		}

		public long Sequence {
			get { return seq; }
			set { seq = value; }
		}

		System.Timers.Timer flusher;
		/**
		 * The front end of the Enqueue/Push/PushBoundry mechanism which can be used to
		 * reduce concurrency locking down to just a simple spin lock test that will
		 * check to see if another thread is already writing.
		 */
		protected void Enqueue( LogRecord rec ) {
			records.Enqueue( rec );
			processor();
			// We need to force some thread to stay here until all records are written
			// once the count of records gets to a particular point.  This loop
			// creates a 
			//while( limiting && records.Count > waitCount + 1 ) {
			//	processor();
			//	Thread.Sleep( 10 );
			//}
		}

		private void AsyncFlush(object sender, System.Timers.ElapsedEventArgs e)
		{
			try {
				while( records.Count > 0 ) {
					processor();
					//Thread.Sleep( 10 );
				}
			} catch( Exception ex ) {
				Console.WriteLine( ex.ToString() );
			}
		}

		// The spin lock used to keep only one thread active.
		private SpinLock _spinlock = new SpinLock();

		/**
		 * The processsing loop with spin lock that will cause only one thread
		 * at a time to be writing.  This can be called by "Flush" and "Close" implementations to
		 * make sure that the queue is empty.  Returns false if there is still I/O being
		 * done by another thread, true when the calling thread took the lock and completed
		 * the loop, thus assuring the queue was empty at that moment.
		 */
		protected bool processor() {
			LogRecord rec;

			// A thread which already holds the lock is somehow recursively reentering
			// and we just need to return in that case because there is nothing for
			// that thread to do here, it is at Push or PushBoundry, and should not
			// need to come back to process more records.
			if( _spinlock.IsHeldByCurrentThread )
				return true;
			bool lockTaken = _spinlock.IsHeldByCurrentThread;

			int cnt = 0;
			try
			{
				if ( consoleDebug )
					Console.WriteLine( "Checking for too many records queued: " + Thread.CurrentThread );
				while( holding && records.Count > holdCount ) {
					if( !lockTaken )
						_spinlock.TryEnter( ref lockTaken );
					if(lockTaken) {
						if ( consoleDebug )
							Console.WriteLine( "Got lock, going to Push out records" );
						break;
					}
					if ( consoleDebug )
						Console.WriteLine( "waiting because the queue is too full : " + Thread.CurrentThread );
					lock( this ) {
						Monitor.Wait( this, 100 );
					}
				}
				if ( consoleDebug )
					Console.WriteLine( "continuing to try lock(lockTaken=" + lockTaken + ") : " + Thread.CurrentThread );
				if ( !lockTaken ) {
					_spinlock.TryEnter( ref lockTaken );
				}
				if(lockTaken) {
					if( flusher != null ) {
						flusher.Stop();
						flusher = null;
					}
					if( consoleDebug )
						Console.WriteLine( "Got lock, trying dequeue and push: " + limiting + ", cnt: " + cnt + ", wait: " + waitCount );
					while( records.TryDequeue( out rec ) ) {
						Push( rec );
						if ( consoleDebug )
							Console.WriteLine( ( ( limiting && cnt > waitCount ) ? "" : "not " ) + "pushing next record : " + Thread.CurrentThread );
						if ( limiting && cnt++ > waitCount )
							break;
					}
					PushBoundry();
					if( records.Count > 0 ) {
						lock( this ) {
							if( flusher != null ) {
								flusher.Stop();
							}
							flusher = new System.Timers.Timer();
							flusher.Elapsed += AsyncFlush;
							flusher.Interval = 2000;
							flusher.Start();
						}
					}
				} else {
					if ( consoleDebug )
						Console.WriteLine( "lock is still busy: " + Thread.CurrentThread );
				}
			} finally {
				if ( lockTaken ) _spinlock.Exit( false );
			}
			return lockTaken;
		}

		/// <summary>
		/// This can be overridden to see the moment that a thread empties the logging queue.  
		/// Typically, this will be used to flush an output stream to disk or otherwise make 
		/// sure it is visible.
		/// </summary>
		protected virtual void PushBoundry() {
		}

		/// <summary>
		/// The Publish(LogRecord) implementation needs to provide an implementation of
		/// this method if Enqueue(LogRecord) is used.  Enqueue'ing threads, will call
		/// this method to push data out for logging. Typically, the normal content of
		/// Publish(LogRecord) would be in Push(LogRecord), and Publish(LogRecord) will
		/// just call Enqueue(LogRecord).
		/// </summary>
		/// <param name="rec"></param>
		protected virtual void Push( LogRecord rec ) {}

		/// <summary>
		/// This is the entry point for publishing records out from the Handler.  
		/// This method, for a console logger, might just be 
		/// 		 
		/// "Console.Write( Formatter.format( record ) );
		/// 		 		
		/// for a file logger, or some other destination which has latency and inherent
		/// synchronization needs, Publish( LogRecord ) might choose to use the 
		/// Enqueue/Push/PushBoundry mechanism to eliminate locking which would otherwise
		/// be needed to keep multiple threads from mucking in the I/O structures simultaneously.
		/// </summary>
		/// <param name="record"></param>
		public abstract void Publish( LogRecord record );

		/// <summary>
		/// Flush the output stream or other path to make sure records are delivered
		/// </summary>
		public abstract void Flush();

		/// <summary>
		/// Close down any outbound data paths
		/// </summary>
		public abstract void Close();

		public ErrorManager getErrorManager() {
			return errorMgr;
		}

		public string Prefix {
			get { return prefix; }
		}

		public string Suffix {
			get { return suffix; }
		}

		public bool HaveSuffix {
			get { return haveSuffix; }
		}

		public bool HavePrefix {
			get { return havePrefix; }
		}

		public Formatter Formatter {
			get { return fmtr; }
			set {
				fmtr = value;
				prefix = fmtr.getHead(this);
				suffix = fmtr.getTail(this);
				haveSuffix = suffix.Length > 0;
				havePrefix = prefix.Length > 0;
			}
		}
		public ErrorManager ErrorManager {
			get { return errorMgr; }
			set { errorMgr = value; }
		}
		public Filter Filter {
			get { return filter; }
			set { filter = value; }
		}
		public Level Level {
			get { return level; }
			set { level = value; }
		}
		public bool isLoggable( LogRecord rec ) {
			if( rec.Level.IntValue < level.IntValue || level == Level.OFF )
				return false;
			Filter f = Filter;
			if( f != null ) 
				return f.isLoggable( rec );
			return true;
		}
		protected void reportError( String msg, Exception ex, int code ) {
			if( errorMgr != null )
				errorMgr.reportError( msg, ex, code );
		}
	}
}
