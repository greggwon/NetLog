using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Seqtech.Logging
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
			level = Level.INFO;
		}

		protected long NextSequence {
			get { lock(this) { return seq++; } }
		}

		public long Sequence {
			get { return seq; }
			set { seq = value; }
		}

		/**
		 * The front end of the Enqueue/Push/PushBoundry mechanism which can be used to
		 * reduce concurrency locking down to just a simple spin lock test that will
		 * check to see if another thread is already writing.
		 */
		protected void Enqueue( LogRecord rec ) {
			/**
			 * It would be tempting, to try to grab the spinlock here, and then
			 * if we have it, just do Push/Pushboundry.  However, that opens the door
			 * for a stalled log entry, because the logged record immediately behind us
			 * might the Enqueue instead, and then there would be no thread, that would
			 * actually go get the enqueued record and publish it.  So, we'd actually
			 * need to do the loop anyway, checking for queued objects, so we might as well
			 * simplify the logic and just pay for the Enqueue/Dequeue for the occasional record
			 * anyway.
			 */
			records.Enqueue( rec );
			processor();
		}

		// The spin lock used to keep only one thread active.
		private SpinLock _spinlock = new SpinLock();

		/**
		 * The processsing loop with spin lock that will cause only one thread
		 * at a time to be writing.
		 */
		private void processor() {
			LogRecord rec;

			bool lockTaken = _spinlock.IsHeldByCurrentThread;
			int cnt = 0;
			try
			{
				if ( consoleDebug )
					Console.WriteLine( "Checking for too many records queued: " + Thread.CurrentThread );
				while( records.Count > holdCount ) {
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
					if ( consoleDebug )
						Console.WriteLine( "Got lock, trying dequeue and push: " + limiting + ", cnt: " + cnt + ", wait: " + waitCount );
					while( records.TryDequeue( out rec ) ) {
						Push( rec );
						if ( consoleDebug )
							Console.WriteLine( ( ( limiting && cnt > waitCount ) ? "" : "not " ) + "pushing next record : " + Thread.CurrentThread );
						if ( limiting && cnt++ > waitCount )
							break;
					}
					PushBoundry();
				} else {
					if ( consoleDebug )
						Console.WriteLine( "lock is still busy: " + Thread.CurrentThread );
				}
			} finally {
				if (lockTaken) _spinlock.Exit(false);
			}
		}

		/**
		 * This can be overridden to see the moment that a thread empties
		 * the logging queue.  Typically, this will be used to flush an output
		 * stream to disk or otherwise make sure it is visible.
		 */
		protected virtual void PushBoundry() {}

		/**
		 * The Publish(LogRecord) implementation needs to provide an implementation of
		 * this method if Enqueue(LogRecord) is used.  Enqueue'ing threads, will call
		 * this method to push data out for logging. Typically, the normal content of
		 * Publish(LogRecord) would be in Push(LogRecord), and Publish(LogRecord) will
		 * just call Enqueue(LogRecord).
		 */
		protected virtual void Push( LogRecord rec ) {}

		/**
		 * This is the entry point for publishing records out from the Handler.  This
		 * method, for a console logger, might just be 
		 *
		 *		"Console.Write( Formatter.format( record ) );
		 *		
		 * for a file logger, or some other destination which has latency and inherent
		 * synchronization needs, Publish( LogRecord ) might choose to use the 
		 * Enqueue/Push/PushBoundry mechanism to eliminate locking which would otherwise
		 * be needed to keep multiple threads from mucking in the I/O structures simultaneously.
		 */
		public abstract void Publish( LogRecord record );


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
		public abstract void Flush();
		public abstract void Close();
	}
}
