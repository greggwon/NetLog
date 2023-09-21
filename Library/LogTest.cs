using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetLog.Logging;
using System.IO;

namespace NetLog.test
{
	public class LogTest
	{
		public static void Main() {
			Logger log = Logger.GetLogger( "NetLog.test.LogText" );
			log.Level = Level.ALL;
			foreach( var h in Logger.GetLogger("").GetHandlers() ) {
				h.Formatter = new StreamFormatter();
				h.Level = Level.ALL;
			}

			log.fine( "log message" );
			log.fine( "log message {0}", "with parameters" );
			log.fine( "log message {0}, {1}", new object[]{ "with", "other data"} );
			log.fine( "something not right", new IOException("some error") );
			log.fine( "something not right: {0}", new IOException( "some error" ), "with a parameter" );
			log.fine( "something not right: {0}", new IOException( "some error" ), new object[]{ "with a parameter" } );
		}
	}
}
