using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seqtech.Logging;

namespace Seqtech.test
{
	public class LogTest
	{
		public static void Main() {
			Logger log = Logger.getLogger( "Seqtech.test.LogText" );
		}
	}
}
