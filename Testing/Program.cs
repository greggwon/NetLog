using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using NetLog.Logging;

namespace NetLog.Logging.Testing {
	class Program  {
		public static void Main(string[] args) {
			Logger log = Logger.GetLogger("iWellScada.logging.kern");
			log.info("kern logging");
		}
	}
}
