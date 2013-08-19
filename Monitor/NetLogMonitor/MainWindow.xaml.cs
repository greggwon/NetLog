using Microsoft.Win32;
using NetLog.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetLog.NetLogMonitor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window {
		TcpClient conn;
		Logger log;
		public MainWindow() {
			log = Logger.GetLogger(GetType().FullName);
			InitializeComponent();
		}

		bool cancelled;

		private Dispatcher disp = Dispatcher.CurrentDispatcher;
		private void Connect_Cancel( object sender, RoutedEventArgs e ) {
			cancelled = true;
			Connect.Click -= Connect_Click;
			Connect.Click -= Connect_Cancel;
			Connect.Click += Connect_Click;
			Connect.IsEnabled = false;
		}

		private void Connect_Click( object sender, RoutedEventArgs e ) {
			if( cancelled ) {
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						Connect.Content = "Connect";
						Connect.Click -= Connect_Click;
						Connect.Click -= Connect_Cancel;
						Connect.Click += Connect_Click;
						Connect.IsEnabled = true;
						cancelled = false;
					})
				);
			}
			int port = 0;
			cancelled = false;
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					Connect.Content = "Cancel";
					Connect.Click -= Connect_Click;
					Connect.Click -= Connect_Cancel;
					Connect.Click += Connect_Cancel;
					port = int.Parse(tcpPort.Text);
				})
			);

			try {
				conn = new TcpClient();
				conn.SendTimeout = 2000;
				conn.BeginConnect("localhost", port, onConnect, conn);
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						textArea.AppendText(String.Format("\n------- Connected to localhost:{0} at {1}\n\n",
							port, DateTime.Now.ToLocalTime() ) );
					}));
			} catch( Exception ex ) {
				log.severe(ex);
				ThreadStart start = delegate() {
					disp.Invoke(DispatcherPriority.Normal,
						new Action(() => {
							Connect.IsEnabled = true;
							MessageBox.Show(String.Format("Error Connecting to localhost:{0}\n\n{1}",
								tcpPort.Text, ex.Message),
								String.Format("Error Connecting to localhost:{0}", tcpPort.Text),
								MessageBoxButton.OK, MessageBoxImage.Warning);
						}));
				};
				new Thread(start).Start();
				return;
			}
		}

		private void onConnect( IAsyncResult ar ) {
			log.info("Connected to " + conn);
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					Connect.Content = "Close";
					Connect.Click -= Connect_Click;
					Connect.Click -= Connect_Cancel;
					Connect.Click += Connect_Cancel;
				})
			);
			Thread th = new Thread(new ThreadStart(() => {
				try {
					log.fine("starting ReceiveData processing");
					ReceiveData();
				} catch( Exception ex ) {
					conn.Close();
					log.severe(ex);
					Connect_Click(null, null);
				}
			}));
			th.IsBackground = true;
			th.Start();
		}

		private void ReceiveData() {
			byte[] data = new byte[ 10240 ];
			int cnt;
			while( !cancelled && ( cnt = conn.GetStream().Read(data, 0, data.Length) ) > 0 ) {
				try {
					string s = System.Text.Encoding.UTF8.GetString(data, 0, cnt);
					s= s.Replace("\r", "");
					s = s.Replace("\n\n", "\n");
					log.fine("Read string {0} chars", s.Length);
					ThreadStart start = delegate() {
						disp.Invoke(DispatcherPriority.Normal,
							new Action(() => {
								log.finer("looking at textArea: {0}", textArea);
								log.fine("inserting {0}", s);
								textArea.AppendText(s);
								textArea.ScrollToEnd();
								//textArea();
							}));
					};
					new Thread(start).Start();
				} catch( Exception ex ) {
					log.severe(ex);
				}
			}
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					Connect.IsEnabled = true;
					Connect.Content = "Connect";
					Connect.Click -= Connect_Click;
					Connect.Click -= Connect_Cancel;
					Connect.Click += Connect_Click;
				})
			);
		}

		private void clearButton_Click( object sender, RoutedEventArgs e ) {
			ThreadStart start = delegate() {
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {

						textArea.Clear();
						//textArea();
					}));
			};
			new Thread(start).Start();

		}
	}
}
