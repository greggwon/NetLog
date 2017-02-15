using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

#if BonjourEnabled
using ZeroconfService;
#endif

namespace NetLog.Logging {
	public class TCPSocketHandler : Handler {
		private ListenerManager listener;
		internal bool listening;
		private int port = 12314;
		private string addr;
#if BonjourEnabled
		private string appName;
#endif
		public TCPSocketHandler( string host, int port )
			: this(port) {
			HostAddress = host;
		}

		public TCPSocketHandler( int port )
			: this( port, true, true, false ) {
		}

		public TCPSocketHandler( int port, bool brief, bool withTime, bool truncatePackageNames )
			: base() {
			PortNumber = port;
			Formatter = new StreamFormatter(brief, withTime, truncatePackageNames );
#if BonjourEnabled
			// setting this causes problems because then we have an explicit bind, and connections to "localhost"
			// don't work because we re bound to ipaddress-any (0.0.0.0).
			//HostAddress = System.Environment.MachineName;
			appName = AppDomain.CurrentDomain.FriendlyName;
#endif
		}

		public TCPSocketHandler()
			: this(12314) {
		}

		/// <summary>
		/// This can be overridden to see the moment that a thread empties the logging queue.  
		/// Typically, this will be used to flush an output stream to disk or otherwise make 
		/// sure it is visible.
		/// </summary>
		protected override void PushBoundry() { 
		}

		/// <summary>
		/// The Publish(LogRecord) implementation needs to call an implementation of
		/// this method if Enqueue(LogRecord) is used.  Enqueue'ing threads, will call
		/// this method to push data out for logging. Typically, the normal content of
		/// Publish(LogRecord) would be in Push(LogRecord), and Publish(LogRecord) will
		/// just call Enqueue(LogRecord).
		/// </summary>
		/// <param name="rec"></param>
		protected override void Push( LogRecord rec ) {
			// stop now if not loggable
			if( rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF ) {
				return;
			}

			rec.SequenceNumber = NextSequence;

			String str = Formatter.format(rec) + "\r";
			try {
				byte[] msg = System.Text.Encoding.UTF8.GetBytes(str);
				List<TcpClient> fails = new List<TcpClient>();
				List<TcpClient> socks;
				lock( listener.remoteSockets ) {
					socks = new List<TcpClient>( listener.remoteSockets );
				}
				foreach( TcpClient remote in socks ) {
					try {
						remote.GetStream().Write( msg, 0, msg.Length );
						remote.GetStream().Flush();
					} catch( Exception ex ) {
						fails.Add(remote);
						LogManager.ReportExceptionToEventLog("Cannot write message to client " + remote + ": " + ex, ex);
					}
				}
				foreach( TcpClient remote in fails ) {
					try {
						remote.Close();
					} catch( Exception ex ) {
						LogManager.ReportExceptionToEventLog("Cannot write message to client " + remote + ": " + ex, ex);
					} finally {
						lock( listener.remoteSockets ) {
							listener.remoteSockets.Remove( remote );
						}
					}
				}
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog( str, ex);
			}
		}

		/// <summary>
		/// This is the entry point for publishing records out from the Handler.  
		/// This method, for a console logger, might just be 
		/// 		 
		/// "Console.Write( Formatter.format( record ) );
		/// 		 		
		/// for a file logger, or some other destination which has latency and inherent
		/// synchronization needs, Publish( LogRecord ) might choose to use the 
		/// Enqueue/Push/PushBoundry mechanism to eliminate locking which would otherwise
		/// be needed to keep multiple threads from mucking in the I/O structures simultaneously.
		/// </summary>
		/// <param name="record"></param>
		public override void Publish( LogRecord record ) {
			// start listener now, if not listening
			if( !listening ) {
				lock( this ) {
					if( !listening ) {
						ListenerManager lm = listener;
						listener = null;
						if( lm != null ) {
							lm.Close();
						}
						listener = new ListenerManager(this);
						listener.HostAddress = HostAddress;
						listener.PortNumber = PortNumber;
						listener.StartListening();
						listening = true;
					}
				}
			}
			Enqueue(record);
		}


		/// <summary>
		/// Flush the output stream or other path to make sure records are delivered
		/// </summary>
		public override void Flush() {
		}

		/// <summary>
		/// Close down any outbound data paths
		/// </summary>
		public override void Close() {
			try {
				if( listener != null )
					listener.Close();
			} finally {
				listening = false;
				listener = null;
			}
		}

		public int PortNumber {
			get {
				return port;
			}
			set {
				port = value;
				if( listener != null )
					listener.PortNumber = port;
			}
		}

		public String HostAddress {
			get {
				return addr;
			}
			set {
				addr = value;
				if( listener != null )
					listener.HostAddress = addr;
			}
		}
#if BonjourEnabled
		public String ApplicationName {
			get {
				return appName;
			}
			set {
				appName = value;
				if( listener != null )
					listener.ApplicationName = appName;
			}
		}
#endif
	}

	internal class ListenerManager : IDisposable {
		internal List<TcpClient>remoteSockets;
		private bool stopping;
		private volatile TCPSocketHandler hand;
		private string addr;
#if BonjourEnabled
		private string type, name;
		private NetService publishService;
		private string domain;
		private string appName;
#endif
		private static Logger log = Logger.GetLogger(typeof(ListenerManager).FullName);

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose( bool all ) {
			if( listener != null )
				listener.Stop();
		}

		public ListenerManager( TCPSocketHandler handler ) {
			if( log.IsLoggable( Level.FINE ) )
				Console.Write( "Creating new listenermanager for: " + handler );
			remoteSockets = new List<TcpClient>();
			stopping = false;
			hand = handler;
			this.HostAddress = handler.HostAddress;
			this.PortNumber = handler.PortNumber;
#if BonjourEnabled
			domain = "local.";
			type = "_Netlog._tcp";
			name = "netlog_logging";
			this.ApplicationName = handler.ApplicationName;
			InitBonjourSetup();
#endif
		}

#if BonjourEnabled
		private void InitBonjourSetup() {
			publishService = new NetService(domain, type, this.name, this.PortNumber);

			publishService.DidPublishService += new NetService.ServicePublished(publishService_DidPublishService);
			publishService.DidNotPublishService += new NetService.ServiceNotPublished(publishService_DidNotPublishService);

			RepublishService();
		}

		private void RepublishService() {
			System.Collections.Hashtable dict = new System.Collections.Hashtable();
			dict.Add("txtvers", "1");
			dict.Add("application", this.ApplicationName);
			dict.Add("host", this.HostAddress);
			dict.Add("port", this.PortNumber);

			publishService.TXTRecordData = NetService.DataFromTXTRecordDictionary(dict);

			publishService.Publish();
		}

		private void publishService_DidNotPublishService( NetService service, DNSServiceException exception ) {
			log.severe("{0}: Service was not published for browsing: {1}", exception, service, exception.Message);
		}

		private void publishService_DidPublishService( NetService service ) {
			throw new NotImplementedException();
		}
#endif

		/// <summary>
		/// Callback delegate for TcpListener accepted connections to TcpClient
		/// </summary>
		/// <param name="cl"></param>
		public delegate void AcceptCallBack( TcpClient cl );

		/// <summary>
		/// Accept callback handler to register socket and start listening for to be implemented
		/// API handling.
		/// </summary>
		/// <param name="remoteSocket"></param>
		internal void acceptCallBack( TcpClient remoteSocket ) {
			lock( remoteSockets ) {
				remoteSockets.Add( remoteSocket );
			}
			// Make sure there is no Nagel holding of writes
			remoteSocket.NoDelay = true;
			WatchLevels( hand, remoteSocket );
		}

		/// <summary>
		/// Process characters sent through the TCP stream, '+' and '-' which will change
		/// the logging level of the handler.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="handler"></param>
		internal void WatchLevels( TCPSocketHandler h, TcpClient handler ) {
			Thread th = new Thread(() => {
				h.Publish(new LogRecord(h.Level, "Processing telnet controls: "+handler.Client.RemoteEndPoint ));
				int cnt;
				byte[] data = new byte[1024];
				try {
					BinaryReader rd = new BinaryReader( handler.GetStream() );
					while( !stopping && handler.Connected && ( cnt = rd.Read( data, 0, data.Length ) ) > 0 ) {
						String str = "";
						for( int i = 0; i < cnt; ++i ) {
							if( i > 0 )
								str += " ";
							str += data[ i ].ToString("x02");
						}
						// Check for a Telnet protocol request and just ignore it.
						switch( data[ 0 ] ) {
							case 0xff:
								h.Publish(new LogRecord(h.Level, "Saw telnet control: " + str));
								continue;
						}

						// Pending work to change protocol between LogMonitor and this class.  The JSON strings
						// assigned to this variable represent the message content proposed to be sent from the
						// clients.
						//string cmdTemplate = "";
						//switch( (NetLogProto)data[ 0 ] ) {
						//	case NetLogProto.SET_LOGGER_CONFIG:
						//		cmdTemplate = "{ 'type' : 'setlogger', 'setlogger' : { 'name' : 'loggername', 'handler' : 'classname', 'formatter' : 'classname', 'level' : 'Level' } }";
						//		break;
						//	case NetLogProto.QUERY_LOGGERS:
						//		cmdTemplate = "{ 'type' : 'getloggers' }";
						//		break;
						//	case NetLogProto.QUERY_LOGGER_HANDLERS:
						//		cmdTemplate = "{ 'type' : 'gethandlers', 'gethandlers' : { 'name' : 'loggername' } }";
						//		break;
						//	case NetLogProto.QUERY_HANDLER_FORMATTER:
						//		cmdTemplate = "{ 'type' : 'getformatter', 'getformatter' : { 'handler' : 'classname' } }";
						//		break;
						//	case NetLogProto.ADD_LOGGER_HANDLER:
						//		cmdTemplate = "{ 'type' : 'sethandler', 'sethandler' : { 'name' : 'loggername', 'handler' : 'classname', 'properties' : { 'name' : 'value' } } }";
						//		break;
						//	case NetLogProto.REMOVE_LOGGER_HANDLER:
						//		cmdTemplate = "{ 'type' : 'rmvhandler', 'rmvhandler' : { 'name' : 'loggername', 'handler' : 'classname' } }";
						//		break;
						//	case NetLogProto.SET_HANDLER_PROPERTIES:
						//		cmdTemplate = "{ 'type' : 'setproperties', 'setproperties' : { 'name' : 'loggername', 'handler' : 'classname', 'properties' : { 'name' : 'value' } } }";
						//		break;
						//	case NetLogProto.QUERY_HANDLER_PROPERTIES:
						//		cmdTemplate = "{ 'type' : 'getproperties', 'getproperties' : { 'name' : 'loggername', 'handler' : 'classname' } }";
						//		break;
						//	case NetLogProto.SET_HANDLER_FORMATTER:
						//		cmdTemplate = "{ 'type' : 'setformatter', 'setformatter' : { 'name' : 'loggername', 'handler' : 'classname', 'formatter' : 'classname', 'properties' : { 'name' : 'value' } } }";
						//		break;
						//	case NetLogProto.SET_LOG_LEVEL:
						//		cmdTemplate = "{ 'type' : 'setlevel', 'setlevel' : { 'name' : 'loggername', 'level' : 'Level' } }";
						//		break;
						//}
					
						h.Publish(new LogRecord(h.Level, "Saw unexpected received data: " + str));
					}
				} catch( Exception ex ) {
					LogManager.ReportExceptionToEventLog("Error listening to socket: " + listener, ex);
				}
			});
			th.IsBackground = true;
			th.Start();
		}

		internal void StartListening() {
			IPEndPoint localEP = null;
			if( log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "StartListening Called: " + Environment.StackTrace );
			if( HostAddress == null ) {
				localEP = new IPEndPoint(IPAddress.Any, PortNumber);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(HostAddress);
				for( int i = 0; i < ipHostInfo.AddressList.Length; ++i ) {
					// binding to IPV4 addresses only, so skip IPV6 addresses
					if( ConfigurationManager.AppSettings[ "Listener:Bind:IPV6" ] == null ) {
						if( ipHostInfo.AddressList[ i ].AddressFamily == AddressFamily.InterNetworkV6 )
							continue;
					}
					localEP = new IPEndPoint(ipHostInfo.AddressList[ i ], PortNumber);
					break;
				}
				if( localEP == null ) {
					if( ipHostInfo != null && ipHostInfo.AddressList != null ) {
						throw new NullReferenceException( "Could not find any (tried " + ipHostInfo.AddressList.Length + ") DNS mappings for: " + HostAddress );
					} else {
						throw new NullReferenceException( "Could not find any DNS mappings for: " + HostAddress );
					}
				}
			}
			listener = new ListenerThread();
			listener.Start( localEP, acceptCallBack );

			if( log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "TCPSocketHandler: Local address and port : {0}", localEP.ToString() );
		}
		private volatile ListenerThread listener;
		public delegate void AcceptCallback( IAsyncResult ar );
		public class ListenerThread {
			private volatile TcpListener listener;
			private volatile bool stopping;

			public void Stop() {
				Console.WriteLine( "ListenerThread was stopped" );
				stopping = true;
				try {
					if( listener != null )
						listener.Stop();
				} catch( Exception ex ) {
					Console.Write( "Error stopping listener: " + ex );
				}
			}

			public void Start( IPEndPoint localEP, AcceptCallBack callBack ) {
				Thread th = new Thread( new ThreadStart( () => {
					try {
						listener = new TcpListener( localEP );
						if( log.IsLoggable( Level.FINE ) )
							Console.WriteLine( "Binding to: " + localEP );
						listener.Start();
						while( !stopping ) {
							try {
								//							Console.WriteLine( "Starting accept loop" );
								// reset semaphore to wait for accept to complete
								Console.WriteLine( "Starting Accept" );
								// Enqueue accept
								TcpClient remoteSocket = listener.AcceptTcpClient();
								if( log.IsLoggable( Level.FINE ) )
									Console.WriteLine( "Received connection from: "+remoteSocket );
								callBack( remoteSocket );
							} catch( Exception ex ) {
								Console.WriteLine( "NN: " + ex.ToString() );
							}
						}
					} catch( Exception e ) {
						Console.WriteLine( "NS: " + e.ToString() );
						LogManager.ReportExceptionToEventLog( "Error Processing Connections to socket: " + listener, e );
					} finally {
						if( listener != null ) {
							try {
								listener.Stop();
							} catch( Exception ex ) {
								Console.WriteLine( "SSEX: " + ex );
							}
						}
						if( log.IsLoggable( Level.FINE ) )
							Console.WriteLine( "Exiting handler listen/accept thread" );
					}
				} ) );
				th.Name = "NetLog Listener: " + localEP.Port;
				th.IsBackground = true;
				th.Start();
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		public void Close() {
			stopping = true;

			if( log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "Closing ServerManager ListenerThread: " + listener );
			try {
				if( listener != null )
					listener.Stop();
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog( "Can't close listener", ex );
			} finally {
				listener = null;
			}

			List<TcpClient> socks;
			lock( remoteSockets ) {
				socks = new List<TcpClient>( remoteSockets );
				// Force NPE for any remaining users.
				remoteSockets = null;
			}
			foreach( TcpClient h in socks ) {
				try {
					if( log.IsLoggable( Level.FINE ) )
						Console.WriteLine( "Closing socket: " + h );
					h.Close();
				} catch( Exception ex ) {
					LogManager.ReportExceptionToEventLog( "Cannot write message to client " + h + ": " + ex, ex );
				}
			}

			if( log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "Closed ServerManager: " + this );
		}

		public string HostAddress {
			get {
				return addr;
			}
			set {
				addr = value;
				reconnect("address change");
			}
		}

		private int port;
		public int PortNumber {
			get {
				return port;
			}
			set {
				if( port != value ) {
					port = value;
					reconnect( "port change" );
				}
			}
		}

		private void reconnect(string why) {
			if( hand == null || hand.listening == false )
				return;
			try {
				Close();
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog("Error shutting down TCPSocketHandler for "+why, ex);
			}
			if( log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "Reconnecting: " + why + ": " + Environment.StackTrace );
			try {
				remoteSockets = new List<TcpClient>();
				StartListening();
#if BonjourEnabled
				RepublishService();
#endif
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog("Error restarting listening for "+why+" on "+addr+", port=" + port, ex);
			}
			
		}
#if BonjourEnabled
		public string ApplicationName {
			get {
				return appName;
			}
			set {
				appName = value;

				//RepublishService();
			}
		}
#endif
	}
}
