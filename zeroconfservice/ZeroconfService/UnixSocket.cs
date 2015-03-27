using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZeroconfService {
	/// <summary>
	/// 
	/// </summary>
	public class UnixSocket {
		private UIntPtr mSocket;

		// Delegate to allow asynchronous calling of the poll method
		private delegate bool AsyncPollCaller( int microSeconds, SelectMode mode );
		private AsyncPollCaller caller;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		public UnixSocket( UIntPtr socket ) {
			mSocket = socket;

			caller = new AsyncPollCaller(Poll);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="microSeconds"></param>
		/// <param name="mode"></param>
		/// <param name="callback"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public virtual IAsyncResult BeginPoll( int microSeconds, SelectMode mode, AsyncCallback callback, Object state ) {
			IAsyncResult result = caller.BeginInvoke(microSeconds, mode, callback, state);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="asyncResult"></param>
		/// <returns></returns>
		public virtual bool EndPoll( IAsyncResult asyncResult ) {
			return caller.EndInvoke(asyncResult);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="microSeconds"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		protected bool Poll( int microSeconds, SelectMode mode ) {
			fd_set readFDs = null;
			fd_set writeFDs = null;

			if( mode == SelectMode.SelectRead ) {
				readFDs = new fd_set();
				readFDs.FD_ZERO();
				readFDs.FD_SET(mSocket);
			}
			if( mode == SelectMode.SelectWrite ) {
				writeFDs = new fd_set();
				writeFDs.FD_ZERO();
				writeFDs.FD_SET(mSocket);
			}

			Int32 ret = select(0, readFDs, null, null, null);

			//Console.WriteLine("select returned: {0}", ret);

			if( readFDs.FD_ISSET(mSocket) ) {
				return true;
			}
			return false;
		}

		/* unmanaged stuff */
		/// <summary>
		/// 
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private class fd_set {
			public UInt32 fd_count;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
			public UIntPtr[] fd_array;

			public fd_set() {
				fd_array = new UIntPtr[ 64 ];
			}

			public void FD_ZERO() {
				fd_count = 0;
			}
			public void FD_SET( UIntPtr fd ) {
				int i;
				for( i = 0; i < fd_count; i++ ) {
					if( fd_array[ i ] == fd ) break;
				}
				if( i == fd_count ) {
					fd_array[ i ] = fd;
					fd_count++;
				}
			}
			public void FD_CLR( UIntPtr fd ) {
				int i;
				for( i = 0; i < fd_count; i++ ) {
					if( fd_array[ i ] == fd ) {
						while( i < ( fd_count - 1 ) ) {
							fd_array[ i ] = fd_array[ i + 1 ];
							i++;
						}
						fd_count--;
						break;
					}
				}
			}
			public bool FD_ISSET( UIntPtr fd ) {
				return ( __WSAFDIsSet(fd, this) != 0 );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private class timeval {
			long a,b;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fd"></param>
		/// <param name="set"></param>
		/// <returns></returns>
		[DllImport("Ws2_32.dll")]
		private static extern Int32 __WSAFDIsSet( UIntPtr fd, fd_set set );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nfds"></param>
		/// <param name="readFDs"></param>
		/// <param name="writeFDs"></param>
		/// <param name="exceptFDs"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		[DllImport("Ws2_32.dll")]
		private static extern Int32 select( Int32 nfds, fd_set readFDs, fd_set writeFDs, fd_set exceptFDs, timeval timeout );

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[DllImport("Ws2_32.dll")]
		private static extern Int32 WSAGetLastError();
	}

	class WatchSocket: UnixSocket {
		private IntPtr sdRef;
		private bool inPoll;
		private bool stopping;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="sdRef"></param>
		public WatchSocket( UIntPtr socket, IntPtr sdRef )
			: base(socket) {
			this.sdRef = sdRef;
			this.inPoll = false;
			this.stopping = false;
		}

		/// <summary>
		/// 
		/// </summary>
		public IntPtr SDRef {
			get { return sdRef; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool InPoll {
			get { return inPoll; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="microSeconds"></param>
		/// <param name="mode"></param>
		/// <param name="callback"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public override IAsyncResult BeginPoll( int microSeconds, SelectMode mode, AsyncCallback callback, Object state ) {
			if( inPoll )
				throw new ApplicationException("Attempting to begin a new poll while already polling.");

			if( stopping )
				throw new ApplicationException("Attempting to begin a new poll after stopping.");

			inPoll = true;
			return base.BeginPoll(microSeconds, mode, callback, state);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="asyncResult"></param>
		/// <returns></returns>
		public override bool EndPoll( IAsyncResult asyncResult ) {
			inPoll = false;
			return base.EndPoll(asyncResult);
		}

		/// <summary>
		/// 
		/// </summary>
		public bool Stopping {
			get { return stopping; }
			set { stopping = value; }
		}
	}
}
