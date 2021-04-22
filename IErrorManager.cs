using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLog.Logging
{
	public interface IErrorManager
	{
		void ReportError( String msg, Exception ex, int code );
	}
}