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
		internal Queue<LogRecord>history = new Queue<LogRecord>();

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
			if( rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF || listener.remoteSockets.Count == 0 ) {
				return;
			}

			rec.SequenceNumber = NextSequence;
			if( listener.remoteSockets.Count == 0 ) {
				lock( this ) {
					history.Enqueue(rec);
					while( history.Count > 100 ) {
						history.Dequeue();
					}
				}
			}
			String str = Formatter.format(rec) + "\r";
			try {
				byte[] msg = System.Text.Encoding.UTF8.GetBytes(str);
				lock( listener.remoteSockets ) {
					List<Socket> fails = new List<Socket>();
					foreach( Socket remote in listener.remoteSockets ) {
						try {
							remote.Send(msg);
						} catch( Exception ex ) {
							fails.Add(remote);
							LogManager.ReportExceptionToEventLog("Cannot write message to client " + remote + ": " + ex, ex);
						}
					}
					foreach( Socket remote in fails ) {
						try {
							remote.Close();
						} catch( Exception ex ) {
							LogManager.ReportExceptionToEventLog("Cannot write message to client " + remote + ": " + ex, ex);
						} finally {
							listener.remoteSockets.Remove(remote);
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
		internal List<Socket>remoteSockets;
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
			if( log != null && log.IsLoggable( Level.FINE ) )
				Console.Write( "Creating new listenermanager for: " + handler );
			remoteSockets = new List<Socket>();
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

		private void acceptCallback( IAsyncResult ar ) {

			try {
				Socket listener = (Socket)ar.AsyncState;
				Socket remoteSocket = listener.EndAccept( ar );
				if( log != null && log.IsLoggable( Level.FINE ) )
					Console.WriteLine( "Accept Callback results: " + ar );
				// Turn off Nagle to get data through on each log message.
				remoteSocket.NoDelay = false;
				lock( remoteSockets ) {
					try {
						hand.Publish( new LogRecord( hand.Level, "have handlers: " + remoteSockets.Count ) );
						if( remoteSockets.Count == 0 ) {
							hand.Publish( new LogRecord( hand.Level, "have history Records: " + hand.history.Count ) );
							foreach( LogRecord rec in hand.history ) {
								String str = hand.Formatter.format( rec ) + "\r";
								try {
									byte[] msg = System.Text.Encoding.UTF8.GetBytes( str );
									remoteSocket.Send( msg );
								} catch( Exception ex ) {
									LogManager.ReportExceptionToEventLog( "Cannot write message to client " + remoteSocket + " : " + ex.Message, ex );
									break;
								}
							}
						}
					} finally {
						remoteSockets.Add( remoteSocket );
						WatchLevels( hand, remoteSocket );
					}
				}
				if( log != null && log.IsLoggable( Level.FINE ) )
					Console.WriteLine( "Accepted new connection, reporting: " + remoteSocket );
			} catch( Exception ex ) {
				Console.WriteLine( "ACC: " + ex );
				// we don't clean up remoteSockets here because it will happen on the next
				// I/O attempt on the socket using exception handling there.
				LogManager.ReportExceptionToEventLog( "Can't process socket connection", ex );
			} finally {
				log.fine( "allDone.Set()" );
				listener.Next();
			}
		}

		/// <summary>
		/// Process characters sent through the TCP stream, '+' and '-' which will change
		/// the logging level of the handler.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="handler"></param>
		internal void WatchLevels( TCPSocketHandler h, Socket handler ) {
			Thread th = new Thread(() => {
				h.Publish(new LogRecord(h.Level, "Processing telnet controls: "+handler));
				int cnt;
				byte[] data = new byte[1024];
				try {
					while( !stopping && handler.Connected && ( cnt = handler.Receive(data) ) > 0 ) {
						String str = "";
						for( int i = 0; i < cnt; ++i ) {
							if( i > 0 )
								str += " ";
							str += data[ i ].ToString("x02");
						}
						switch( data[ 0 ] ) {
							case 0xff:
								h.Publish(new LogRecord(h.Level, "Saw telnet control: " + str));
								continue;
						//	case NetLogProto.SET_LOGGER_CONFIG:
						//	case NetLogProto.QUERY_LOGGERS:
						//	case NetLogProto.QUERY_LOGGER_HANDLERS:
						//	case NetLogProto.QUERY_HANDLER_FORMATTER:
						//	case NetLogProto.ADD_LOGGER_HANDLER:
						//	case NetLogProto.REVOVE_LOGGER_HANDLER:
						//	case NetLogProto.SET_HANDLER_PROPERTIES:
						//	case NetLogProto.QUERY_HANDLER_PROPERTIES:
						//	case NetLogProto.SET_HANDLER_FORMATTER:
						//	case NetLogProto.SET_LOG_LEVEL:
						//		break;
						}
					
						h.Publish(new LogRecord(h.Level, "Saw unexpected received data: " + str));
					}
				} catch( Exception ex ) {
					LogManager.ReportExceptionToEventLog("Error listening to socket: " + listener, ex);
				}
			});
			th.IsBackground = true;
			th.Start();
		}
		IPEndPoint localEP;

		internal void StartListening() {
			if( log != null && log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "StartListening Called: " + Environment.StackTrace );
			if( HostAddress == null ) {
				localEP = new IPEndPoint(IPAddress.Any, PortNumber);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(HostAddress);
				for( int i = 0; i < ipHostInfo.AddressList.Length; ++i ) {
					if( ipHostInfo.AddressList[ i ].AddressFamily == AddressFamily.InterNetworkV6 )
						continue;
					localEP = new IPEndPoint(ipHostInfo.AddressList[ i ], PortNumber);
					break;
				}
			}
			listener = new ListenerThread();
			listener.Start( localEP, acceptCallback );

			if( log != null && log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "TCPSocketHandler: Local address and port : {0}", localEP.ToString() );
		}
		private volatile ListenerThread listener;
		public delegate void AcceptCallback( IAsyncResult ar );
		public class ListenerThread {
			private volatile Socket listener;
			private volatile bool stopping;
			private AutoResetEvent allDone = new AutoResetEvent( false );
			private AutoResetEvent hasStoppped = new AutoResetEvent( false );
			public void Stop() {
				if( log != null && log.IsLoggable( Level.FINE ) )
					Console.WriteLine( "ListenerThread was stopped" );
				stopping = true;
				allDone.Set();
				hasStoppped.WaitOne();
			}

			public void Start( IPEndPoint localEP, AcceptCallback acceptCallback ) {
				listener = new Socket( localEP.Address.AddressFamily,
					SocketType.Stream, ProtocolType.Tcp );
				stopping = false;
				hasStoppped.Reset();
				Thread th = new Thread( new ThreadStart( () => {
					try {
						if( log != null && log.IsLoggable( Level.FINE ) )
							Console.WriteLine( "Binding to: " + localEP );
						listener.Bind( localEP );
						listener.Listen( 10 );

						while( !stopping ) {
							try {
								//							Console.WriteLine( "Starting accept loop" );
								// reset semaphore to wait for accept to complete
								log.fine( "allDone.Reset()" );
								if( log != null && log.IsLoggable( Level.FINE ) )
									Console.WriteLine( "Resettting allDone" );
								allDone.Reset();

								if( log != null && log.IsLoggable( Level.FINE ) )
									Console.WriteLine( "Starting Accept" );
								// Enqueue accept
								listener.BeginAccept(
									new AsyncCallback( acceptCallback ),
									listener );

								// Wait on semaphore.
								if( log != null && log.IsLoggable( Level.FINE ) )
									Console.WriteLine( "Waiting for allDone" );
								log.fine( "allDone.WaitOne()" );
								allDone.WaitOne();
								if( log != null && log.IsLoggable( Level.FINE ) )
									Console.WriteLine( "Completed for allDone: " + stopping );
							} catch( Exception ex ) {
								Console.WriteLine( "NN: " + ex.ToString() );
								listener.Close();
								listener = null;
							}
						}
					} catch( Exception e ) {
						Console.WriteLine( "NS: " + e.ToString() );
						try {
							if( listener != null )
								listener.Close();
						} catch( Exception exx ) {
							Console.WriteLine( "BEX: " + exx );
						} finally {
							listener = null;
						}
						LogManager.ReportExceptionToEventLog( "Error Processing Connections to socket: " + listener, e );
					} finally {
						if( listener != null ) {
							try {
								listener.Close();
							} catch( Exception ex ) {
								Console.WriteLine( "SSEX: " + ex );
							}
						}
						if( log != null && log.IsLoggable( Level.FINE ) )
							Console.WriteLine( "Exiting handler listen/accept thread" );
						hasStoppped.Set();
					}
				} ) );
				th.IsBackground = true;
				th.Start();
			}

			internal void Next() {
				allDone.Set();
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		public void Close() {
			stopping = true;
		
			if( log != null && log.IsLoggable( Level.FINE ) )
				Console.WriteLine( "Closing ServerManager ListenerThread: " + listener );
			try {
				if( listener != null )
					listener.Stop();
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog( "Can't close listener", ex );
			} finally {
				listener = null;
			}

			try {
				foreach( Socket h in remoteSockets ) {
					try {
						Console.WriteLine( "Closing socket: " + h );
						h.Close();
					} catch( Exception ex ) {
						LogManager.ReportExceptionToEventLog( "Cannot write message to client " + h + ": " + ex, ex );
					}
				}
			} finally {
				remoteSockets.Clear();
			}

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
			Console.WriteLine( "Reconnecting: " + why+": "+Environment.StackTrace );
			try {
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
