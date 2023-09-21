using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLog.Logging {
	public enum NetLogProto {
		SET_LOGGER_CONFIG,
		QUERY_LOGGERS,
		QUERY_LOGGER_HANDLERS,
		QUERY_HANDLER_FORMATTER,
		ADD_LOGGER_HANDLER,
		REVOVE_LOGGER_HANDLER,
		SET_HANDLER_PROPERTIES,
		QUERY_HANDLER_PROPERTIES,
		SET_HANDLER_FORMATTER,
	}
}
