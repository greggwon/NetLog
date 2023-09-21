using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLog.Logging
{
	public class LevelFilter : Filter
	{
		private Level level;

		public LevelFilter( Level l ) {
			level = l;
		}

		public bool isLoggable( LogRecord rec ) {
			return rec.Level.IntValue >= level.IntValue || level == Level.OFF;
		}
	}
}
