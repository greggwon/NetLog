using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetLog.Logging
{
	public class LogRecord
	{
		private Level level;
		private string msg;
		private Exception thrownEx;
		private long seq;

		public Level Level { 
			get { return level; } 
			set { level = value; } 
		}
		public string getMessage() { return msg; }
		public LogRecord( Level level, string message ) {
			this.level = level;
			this.msg = message;
			millis = DateTime.Now;
		}

		private string loggerName;
		public string LoggerName {
			get { return loggerName; }
			set { loggerName = value; }
		}
		private DateTime millis;
		public DateTime Millis {
			get { return millis; }
			set { millis = value; }
		}
		private object[] parms;
		public object[] Parameters {
			get { return parms; }
			set { parms = value; }
		}

		public long SequenceNumber {
			get { lock(this) { return seq; } }
			set { seq = value; }
		}
		private string srcCls;
		public string SourceClassName {
			get { return srcCls; }
			set { srcCls = value; }
		}
		private string srcMeth;
		public String SourceMethodName {
			get { return srcMeth; }
			set { srcMeth = value; }
		}
		private int thid;
		public int ThreadId {
			get { return thid; }
			set { thid = value; }
		}
		public Exception Thrown {
			get { return thrownEx; }
			set { thrownEx = value; }
		}
	}
}
