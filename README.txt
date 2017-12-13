This assembly provides a logging API for .net which is based off of the java.util.logging APIs in operational style.
It includes Logger, Handler and Formatter as well as a LogManager.  Many of the nuances of java.util.logging revolve
around extensions in the form of a different LogManager, or Handlers and Formatters. There is not support for plugging
in a different LogManager at this point.

But, the logging.properties file in the processes current directory can be used to specify the list of Handler
implementations that are associated with the Root ("") Logger instance, as well as each ones Level and Formatter.

The logging.properties file can also be specified by the "netlog.logging.config.file" environment variable or the
"netlog.logging.config.file" app settings property.  The environment variable take precidence over the app settings 
value.

There is a configurationmanager section as well which can be used to create a more complex handler mapping than 
the logging.properties file structure allows.