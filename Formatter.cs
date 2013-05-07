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
		 * The default formatting does nothing but return the message field.
		 */
		public string formatMessage( LogRecord record ) {
			return record.getMessage();
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
