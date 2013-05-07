using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seqtech.Logging
{
	public interface ErrorManager
	{
		void reportError( String msg, Exception ex, int code );
	}
}
