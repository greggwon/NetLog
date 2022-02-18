using System;
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
			Formatter = new StreamFormatter(false, true, false);
		}
		public override void Close() {
		}
		public override void Flush() {
			Console.Out.Flush();
		}
		public override void Publish( LogRecord rec ) {
			// stop now if not loggable
			if (consoleDebug)
				Console.WriteLine("rec level: " + rec.Level + ", our Level: " + this.Level);

			if ( rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF)
			{
				return;
			}

			lock( this ) {
				rec.SequenceNumber = NextSequence;
				if( HavePrefix )
					Console.Write( Prefix );
				Console.Write(this.Formatter.format(rec));
				if ( HaveSuffix )
					Console.Write( Suffix );
			}
		}
	}
}
