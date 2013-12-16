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
		internal List<LogRecord>history = new List<LogRecord>();

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
			HostAddress = null;
			Formatter = new StreamFormatter(brief, withTime, truncatePackageNames );
			//appName = AppDomain.CurrentDomain.ApplicationIdentity.FullName;
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
					history.Add(rec);
					if( history.Count > 100 ) {
						history.RemoveAt(0);
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
		private Socket listener;
		internal List<Socket>remoteSockets;
		private bool stopping;
		private TCPSocketHandler hand;
		private AutoResetEvent allDone = new AutoResetEvent(false);
		private string addr;
		private string type, name;
#if BonjourEnabled
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
				listener.Dispose();
		}

		public ListenerManager( TCPSocketHandler handler ) {
			remoteSockets = new List<Socket>();
			stopping = false;
			hand = handler;
#if BonjourEnabled
			InitBonjourSetup();
#endif
		}

#if BonjourEnabled
		private void InitBonjourSetup() {
			publishService = new NetService(domain, type, name, port);

			publishService.DidPublishService += new NetService.ServicePublished(publishService_DidPublishService);
			publishService.DidNotPublishService += new NetService.ServiceNotPublished(publishService_DidNotPublishService);
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
				Socket remoteSocket = listener.EndAccept(ar);
				// Turn off Nagle to get data through on each log message.
				remoteSocket.NoDelay = false;
				lock( remoteSockets ) {
					try {
						hand.Publish(new LogRecord(hand.Level, "have handlers: " + remoteSockets.Count));
						if( remoteSockets.Count == 0 ) {
							hand.Publish(new LogRecord(hand.Level, "have history Records: " + hand.history.Count));
							foreach( LogRecord rec in hand.history ) {
								String str = hand.Formatter.format(rec) + "\r";
								try {
									byte[] msg = System.Text.Encoding.UTF8.GetBytes(str);
									remoteSocket.Send(msg);
								} catch( Exception ex ) {
									LogManager.ReportExceptionToEventLog("Cannot write message to client " + remoteSocket + " : " + ex.Message, ex);
									break;
								}
							}
						}
					} finally {
						remoteSockets.Add(remoteSocket);
						WatchLevels(hand, remoteSocket);
					}
				}
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog("Can't process socket connection", ex);
			} finally {
				log.info("allDone.Set()");
				allDone.Set();
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
				byte[] data = new byte[10];
				try {
					while( !stopping && handler.Connected && ( cnt = handler.Receive(data) ) > 0 ) {
						String str = "";
						for( int i = 0; i < cnt; ++i ) {
							if( i > 0 )
								str += " ";
							str += data[ i ].ToString("x02");
						}
						if( data[ 0 ] == 0xff ) {
							h.Publish(new LogRecord(h.Level, "Saw telnet control: " + str));
							continue;
						}
						h.Publish(new LogRecord(h.Level, "Saw unexpected received data: " + str));
						for( int i = 0; i < cnt; ++i ) {
							Level l = h.Level;
							if( l == Level.ALL )
								l = Level.FINEST;
							if( l.IntValue > Level.SEVERE.IntValue + 100 ) {
								l = new Level("Level_" + ( Level.SEVERE.IntValue + 100 ), Level.SEVERE.IntValue + 100);
							}
							if( data[ i ] == '+' ) {
								h.Level = new Level("Level_" + ( l.IntValue + 100 ), l.IntValue + 100);
								h.Publish(new LogRecord(h.Level, "Log Level Changed to " + h.Level));
								break;
							} else if( data[ i ] == '=' ) {
								h.Publish(new LogRecord(h.Level, "Log Level is Currently " + h.Level));
							} else if( data[ i ] == '-' ) {
								h.Level = new Level("Level_" + ( l.IntValue - 100 ), l.IntValue - 100);
								h.Publish(new LogRecord(h.Level, "Log Level Changed to " + h.Level));
								break;
							}
						}
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

			Console.WriteLine("Local address and port : {0}", localEP.ToString());

			listener = new Socket(localEP.Address.AddressFamily,
				SocketType.Stream, ProtocolType.Tcp);
			stopping = false;
			Thread th = new Thread(new ThreadStart(() => {
				try {
					listener.Bind(localEP);
					listener.Listen(10);

					while( !stopping ) {
						// reset semaphore to wait for accept to complete
						log.info("allDone.Reset()");
						allDone.Reset();

						// Enqueue accept
						listener.BeginAccept(
							new AsyncCallback(acceptCallback),
							listener);

						// Wait on semaphore.
						log.info("allDone.WaitOne()");
						allDone.WaitOne();
					}
				} catch( Exception e ) {
					LogManager.ReportExceptionToEventLog("Error Processing Connections to socket: " + listener, e);
				}
			}));
			th.IsBackground = true;
			th.Start();
		}
		
		/// <summary>
		/// 
		/// </summary>
		public void Close() {
			stopping = true;

			try {
				if( listener != null )
					listener.Close();
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog("Can't close listener", ex);
			}
			listener = null;

			try {
				foreach( Socket h in remoteSockets ) {
					try {
						h.Close();
					} catch( Exception ex ) {
						LogManager.ReportExceptionToEventLog("Cannot write message to client " + h + ": " + ex, ex);
					}
				}
			} finally {
				remoteSockets.Clear();
			}
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
				port = value;
				reconnect("port change");
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

				RepublishService();
			}
		}
#endif
	}
}
