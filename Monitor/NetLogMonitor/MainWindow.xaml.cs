using Microsoft.Win32;
using NetLog.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
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
		MyLogger log;
		bool trimmingLines, scrollEnd = true;
		public MainWindow() {
			Logger.GetLogger("").info("Logging Loaded");
			log = new MyLogger(GetType().FullName);
			//log.level = Level.SEVERE;
			InitializeComponent();
			int defPort = 12314;
			try {
				string port = ConfigurationManager.AppSettings[ "portNumber" ];
				if( port != null ) {
					defPort = int.Parse(port);
				}
			} catch( Exception ex ) {
				log.severe(ex);
			}

			tcpPort.Text = "" + defPort;
			log.info("starting up");
			BoxRichText.Background = Brushes.LightGray;
			Style noSpaceStyle = new Style(typeof(Paragraph));
			noSpaceStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0)));
			BoxRichText.Resources.Add(typeof(Paragraph), noSpaceStyle);
		}

		private volatile bool cancelled;

		private Dispatcher disp = Dispatcher.CurrentDispatcher;
		private void Connect_Cancel( object sender, RoutedEventArgs e ) {
			cancelled = true;
			Connect.Click -= Connect_Click;
			Connect.Click -= Connect_Cancel;
			Connect.Click += Connect_Click;
			BoxRichText.Background = Brushes.LightGray;
			Connect.IsEnabled = false;
			try {
				conn.Close();
			} catch( Exception ex ) {
				log.severe(ex);
			}
		}

		private void Connect_Click( object sender, RoutedEventArgs e ) {
			int port = 0;
			cancelled = false;
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					Connect.Content = "Cancel";
					Connect.Click -= Connect_Click;
					Connect.Click -= Connect_Cancel;
					Connect.Click += Connect_Cancel;
					try {
						port = int.Parse(tcpPort.Text);
						tcpPort.Background = Brushes.White;
					} catch( Exception ex ) {
						log.severe(ex);
						tcpPort.Background = Brushes.Red;
					}
					BoxRichText.Background = Brushes.GreenYellow;
				})
			);

			try {
				conn = new TcpClient();
				conn.SendTimeout = 2000;
				conn.BeginConnect("localhost", port, onConnect, conn);
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						String str = String.Format("\n------- Connecting to localhost:{0} at {1}...",
							port, DateTime.Now.ToLocalTime());
						//textArea.AppendText(str );
						BoxRichText.AppendText(str);
					}));
			} catch( Exception ex ) {
				log.severe(ex);
				ThreadStart start = delegate() {
					disp.Invoke(DispatcherPriority.Normal,
						new Action(() => {
							String str;
							Connect.IsEnabled = true;
							MessageBox.Show(str = String.Format("Error Connecting to localhost:{0}\n\n{1}",
								tcpPort.Text, ex.Message),
								String.Format("Error Connecting to localhost:{0}", tcpPort.Text),
								MessageBoxButton.OK, MessageBoxImage.Warning);
							BoxRichText.AppendText(str);
						}));
				};
				new Thread(start).Start();
				return;
			}
		}

		private void onConnect( IAsyncResult ar ) {
			if( !ar.IsCompleted ) {
				log.info("AsyncResult is not completed");

			}
			if( !cancelled && conn.Connected ) {
				log.info("Completed connection: {0}", conn.Connected );
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						string str = String.Format("Connected\n\n");
						BoxRichText.AppendText(str);
//						textArea.AppendText(str);
					}));
				log.info("Connected to " + conn);
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						Connect.Content = "Close";
						Connect.Click -= Connect_Click;
						Connect.Click -= Connect_Cancel;
						Connect.Click += Connect_Cancel;
						BoxRichText.Background = Brushes.White;
					})
				);
				Thread th = new Thread(new ThreadStart(() => {
					try {
						log.fine("starting ReceiveData processing");
						ReceiveData();
					} catch( Exception ex ) {
						conn.Close();
						log.severe(ex);
						if( !cancelled )
							Connect_Click(null, null);
					} finally {
						disp.Invoke(DispatcherPriority.Normal,
							new Action(() => {
								Connect.IsEnabled = true;
								Connect.Content = "Connect";
								Connect.Click -= Connect_Click;
								Connect.Click -= Connect_Cancel;
								Connect.Click += Connect_Click;
								BoxRichText.Background = Brushes.LightGray;
							})
						);
					}
				}));
				th.IsBackground = true;
				th.Start();
			} else {
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						string str = String.Format("Failed");
//						textArea.AppendText(str);
						BoxRichText.AppendText(str);
					}));
				if( !cancelled ) {
					try {
						conn.Close();
					} catch { }
					Connect_Click(null, null);
				} else {
					disp.Invoke(DispatcherPriority.Normal,
						new Action(() => {
							Connect.Content = "Connect";
							Connect.Click -= Connect_Click;
							Connect.Click -= Connect_Cancel;
							Connect.Click += Connect_Click;
							Connect.IsEnabled = true;
							cancelled = false;
							BoxRichText.Background = Brushes.LightGray;
						})
					);
				}
			}
		}

		private void ReceiveData() {
			byte[] data = new byte[ 10240 ];
			int cnt;
			NetworkStream stream = conn.GetStream();
//			int cntt =0;
			while( !cancelled && (cnt = stream.Read(data, 0, data.Length) ) > 0 ) {
				//Thread.Sleep(250);
				//data = new byte[] { (byte)'T', (byte)'h', (byte)'i', (byte)'s', (byte)' ', (byte)'i', (byte)'s', (byte)' ', (byte)'i', (byte)'t',(byte)'\n' };
				//cnt = data.Length;
				try {
					string s = System.Text.Encoding.UTF8.GetString(data, 0, cnt);
					s = s.Replace("\r", "");
					s = s.Replace("\n\n", "\n");
					//log.fine("Read string {0} chars", s.Length);
					//s = String.Format("{0}: {1}", cntt++, s);
					disp.Invoke(DispatcherPriority.Normal,
						new Action(() => {
							//log.finer("looking at textArea: {0}", BoxRichText);
							//log.fine("inserting {0}", s);

							double before = BoxRichText.VerticalOffset;
							BoxRichText.AppendText(s);

							if( trimmingLines ) {
								int maxLines = 20;

								try {
									maxLines = int.Parse(trimLineCount.Text);
								} catch( Exception ex ) {
									log.fine(ex);
									return;
								}
								BoxRichText.Document.LineStackingStrategy = LineStackingStrategy.MaxHeight;

								FlowDocument fd = (FlowDocument)BoxRichText.Document;
								TextPointer start = fd.ContentStart;
								TextPointer end = fd.ContentEnd;
								TextPointer back = end;
								TextPointerContext context = end.GetPointerContext(LogicalDirection.Backward);
								Run run = end.Parent as Run;
								bool tooFew = start.GetOffsetToPosition(end) < maxLines;
								back = back.GetPositionAtOffset(-maxLines);
								if( !tooFew && back != null ) {
									back = back.GetLineStartPosition(0);
									if( back != null ) {
										TextRange range = new TextRange(start, back);
										range.Text = "";
									}
								}
							}

							if( scrollEnd ) {
								BoxRichText.ScrollToEnd();
							} else {
								BoxRichText.ScrollToVerticalOffset(before);
							}
						}));
					
				} catch( Exception ex ) {
					log.severe(ex);
				}
			}
	
		}

		private void clearButton_Click( object sender, RoutedEventArgs e ) {
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					FlowDocument fd = (FlowDocument)BoxRichText.Document;
					TextPointer start = fd.ContentStart;
					TextPointer end = fd.ContentEnd;
					new TextRange(start, end).Text = "";
				}));
		}

		private void trimLinesTo_Checked( object sender, RoutedEventArgs e ) {
			trimmingLines = trimLinesTo.IsChecked.Value;
		}

		private void scrollToEnd_Checked( object sender, RoutedEventArgs e ) {
			scrollEnd = scrollToEnd.IsChecked.Value;
		}
	}
	public class MyLogger {
		Handler h;
		public Level level { get; set; }
		public MyLogger( string name ) {
			h = new ConsoleHandler();
			level = Level.INFO;
			( (StreamFormatter)h.Formatter ).Eol = "\n";
		}

		internal void severe( Exception ex ) {
			if( isLoggable(Level.SEVERE) )
				h.Publish(new LogRecord(Level.SEVERE, ex)); 
		}

		private bool isLoggable( Level level ) {
			return this.level.IntValue <= level.IntValue;
		}

		internal void info( string str, params object[] args ) {
			if( isLoggable(Level.INFO) )
				h.Publish(new LogRecord(Level.INFO, str, args));
		}

		internal void fine( string str, params object[] args ) {
			if( isLoggable(Level.FINE) )
				h.Publish(new LogRecord(Level.FINE, str, args));
		}

		internal void warning( string str, params object[] args ) {
			if( isLoggable(Level.WARNING) )
				h.Publish(new LogRecord(Level.WARNING, str, args));
		}

		internal void fine( Exception ex ) {
			if( isLoggable(Level.FINE) )
				h.Publish(new LogRecord(Level.FINE, ex, ex.Message));
		}

		internal void finer( Exception ex ) {
			if( isLoggable(Level.FINER) )
				h.Publish(new LogRecord(Level.FINER, ex, ex.Message));
		}

		internal void finest( Exception ex ) {
			if( isLoggable(Level.FINEST) )
				h.Publish(new LogRecord(Level.FINEST, ex, ex.Message));
		}

		internal void finer( string str, params object[] args ) {
			if( isLoggable(Level.FINER) )
				h.Publish(new LogRecord(Level.FINER, str, args));
		}

		internal void finest( string str, params object[] args ) {
			if( isLoggable(Level.FINEST) )
				h.Publish(new LogRecord(Level.FINEST, str, args));
		}
	}
}
