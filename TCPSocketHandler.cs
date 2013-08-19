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

namespace NetLog.Logging {
	public class TCPSocketHandler : Handler {
		private ListenerManager listener;
		private bool listening;
		private int port;
		private string addr;

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
			if( rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF || listener.handlers.Count == 0 ) {
				return;
			}

			rec.SequenceNumber = NextSequence;
			String str = Formatter.format(rec) +"\r";
			try {
				byte[] msg = System.Text.Encoding.UTF8.GetBytes(str);
				lock( listener.handlers ) {
					List<Socket> fails = new List<Socket>();
					foreach( Socket h in listener.handlers ) {
						try {
							h.Send(msg);
						} catch( Exception ex ) {
							fails.Add(h);
							LogManager.ReportExceptionToEventLog("Cannot write message to client " + h + ": " + ex, ex);
						}
					}
					foreach( Socket h in fails ) {
						try {
							h.Close();
						} catch( Exception ex ) {
							LogManager.ReportExceptionToEventLog("Cannot write message to client " + h + ": " + ex, ex);
						} finally {
							listener.handlers.Remove(h);
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
	}

	internal class ListenerManager : IDisposable {
		private Socket listener;
		internal List<Socket>handlers;
		private bool stopping;
		private TCPSocketHandler hand;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose( bool all ) {
			if( listener != null )
				listener.Dispose();
		}

		public ListenerManager( TCPSocketHandler handler ) {
			handlers = new List<Socket>();
			stopping = false;
			hand = handler;
		}

		private void acceptCallback( IAsyncResult ar ) {
			Socket listener = (Socket)ar.AsyncState;
			Socket handler = listener.EndAccept(ar);
			// Turn off Nagle to get data through on each log message.
			handler.NoDelay = false;
			lock( handlers ) {
				//byte[] data = { 0xff, 253 , 0xff, 240 };
				//handler.Send(data);
				handlers.Add(handler);
				WatchLevels(hand, handler);
			}
			allDone.Set();
		}

		internal void WatchLevels( TCPSocketHandler h, Socket handler ) {
			Thread th = new Thread(new ThreadStart(() => {
				h.Publish(new LogRecord(h.Level, "Processing telnet controls: "+handler));
				int cnt;
				byte[] data = new byte[10];
				while( ( cnt = handler.Receive(data) ) > 0 ) {
					if( data[ 0 ] == 0xff ) {
						String str = "";
						for( int i = 0; i < cnt; ++i ) {
							if( i > 0 )
								str += " ";
							str += data[ i ].ToString("x02");
						}
						h.Publish(new LogRecord(h.Level, "Saw telnet control: " + str));
						continue;
					}
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
			}));
			th.IsBackground = true;
			th.Start();
		}

		AutoResetEvent allDone = new AutoResetEvent(false);
		internal void StartListening() {
			IPEndPoint localEP;
			if( HostAddress == null ) {
				localEP = new IPEndPoint(IPAddress.Any, PortNumber);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(HostAddress);
				localEP = new IPEndPoint(ipHostInfo.AddressList[ 0 ], PortNumber);
			}

			Console.WriteLine("Local address and port : {0}", localEP.ToString());

			listener = new Socket(localEP.Address.AddressFamily,
				SocketType.Stream, ProtocolType.Tcp);

			Thread th = new Thread(new ThreadStart(() => {
				try {
					listener.Bind(localEP);
					listener.Listen(10);

					while( !stopping ) {
						allDone.Reset();
						listener.BeginAccept(
							new AsyncCallback(acceptCallback),
							listener);
						allDone.WaitOne();
					}
				} catch( Exception e ) {
					LogManager.ReportExceptionToEventLog("Error Processing Connections to socket: " + listener, e);
				}
			}));
			th.IsBackground = true;
			th.Start();
		}

		public void Close() {
			stopping = true;
	
			listener.Close();
			foreach( Socket h in handlers ) {
				try {
					h.Close();
				} catch( Exception ex ) {
					LogManager.ReportExceptionToEventLog("Cannot write message to client " + h + ": " + ex, ex);
				}
			}
		}

		public string HostAddress { get; set; }

		public int PortNumber { get; set; }
	}
}
