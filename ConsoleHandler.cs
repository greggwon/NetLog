using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLog.Logging
{
	public class ConsoleHandler : Handler
	{
		private bool consoleDebug;

		public new bool ConsoleDebug
		{
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}
		public ConsoleHandler()
		{
			WaitCount = 100;
		}
		public override void Close() {
		}
		public override void Flush() {
			Console.Out.Flush();
		}
		Queue recs = new Queue();
		public override void Publish( LogRecord rec ) {
			// stop now if not loggable
			if (consoleDebug)
				Console.WriteLine("rec level: " + rec.Level + ", our Level: " + this.Level);

			if ( rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF)
			{
				return;
			}

			Enqueue( rec );
		}

		StringBuilder b = new StringBuilder();
		protected override void PushBoundry() {
			Console.Write( b.ToString() );
			b.Clear();			
		}

		protected override void Push( LogRecord rec ) {
			if( HavePrefix )
				b.Append( Prefix );
				rec.SequenceNumber = NextSequence;
			b.Append( Formatter.format( rec ) );
				if ( HaveSuffix )
				b.Append( Suffix );
			}
		}
	}
