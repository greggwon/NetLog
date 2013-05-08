using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seqtech.Logging
{
	public abstract class Formatter
	{
		protected Formatter() {}

		/**
		 * Provide this implementation to format the whole, logged record
		 * You need to provide any linefeed sequences needed, as the Handler
		 * implementations will not do that, even for the end of the line.
		 */
		public abstract string format( LogRecord record );
	
		/**
		 *  Uses String.Format( record.getMessage(), record.Parameters ), if parametrs are
		 *  provided, otherwise returns record.getMessage().
		 */
		public string formatMessage( LogRecord record ) {
			if( record.Parameters == null || record.Parameters.Count() == 0 )
				return record.getMessage();
			return String.Format( record.getMessage(), record.Parameters );
		}

		/**
		 * A prefix can be provided here, which will be attached to the logged data stream,
		 * by the passed Handler.
		 */
		public string getHead( Handler h ) {
			return "";
		}

		/**
		 * A suffix can be provided here, which will be attached to the logged data stream,
		 * by the passed Handler.
		 */
		public string getTail( Handler h ) {
			return "";
		}
	}
}
