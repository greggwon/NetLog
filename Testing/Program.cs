using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLog.Logging;
using System.Configuration;

namespace Testing {
	class Program {
		static void Main( string[] args ) {
			NetLogConfigurationSection cfg = (NetLogConfigurationSection)ConfigurationManager.GetSection("NetLogConfigurationSection");
			Logger log = Logger.GetLogger(typeof(Program).FullName);
			log.info("got config: {0}", cfg);
		}
	}
}
