using Microsoft.Win32;
using NetLog.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0017 // Naming Styles
#pragma warning disable IDE0044 // Naming Styles

namespace NetLog.NetLogMonitor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window {
		TcpClient conn;
		static Logger log;
		Boolean dirty;
		bool trimmingLines, scrollEnd = true;
		static ListBox theEventList;
		public MainWindow() {
			InitializeComponent();
			eventList.FontSize = 13;
			System.Drawing.RectangleF rectF = new System.Drawing.RectangleF( 0, 0, 40, 40 );
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap( 40, 40, System.Drawing.Imaging.PixelFormat.Format24bppRgb );
			System.Drawing.Graphics g = System.Drawing.Graphics.FromImage( bitmap );
			g.FillRectangle( System.Drawing.Brushes.AliceBlue, 0, 0, 40, 40 );
			g.DrawRectangle( new System.Drawing.Pen( System.Drawing.Brushes.Black, 2 ), 1, 1, 38, 18 );
			g.FillEllipse( System.Drawing.Brushes.LightSalmon, 1, 28, 10, 10 );
			g.FillEllipse( System.Drawing.Brushes.LightSalmon, 15, 28, 10, 10 );
			g.FillEllipse( System.Drawing.Brushes.LightSalmon, 29, 28, 10, 10 );
			int w = 5;
			string tl = ConfigurationManager.AppSettings[ "InstanceName" ];
			for( int s = 5; s < 32; ++s ) {
				System.Drawing.Font f = new System.Drawing.Font( "Arial", s );
				System.Drawing.SizeF sz = g.MeasureString( tl, f );
				if( sz.Width > 38 || sz.Height > 18 )
					break;
				w = s;
			}
			System.Drawing.Font tf = new System.Drawing.Font( "Arial", w );
			System.Drawing.SizeF tsz = g.MeasureString( tl, tf );
			g.DrawString( tl,
				tf,
				System.Drawing.Brushes.Black,
				new System.Drawing.PointF( (38-tsz.Width)/2, (18-tsz.Height)/2 ) );

			IntPtr hBitmap = bitmap.GetHbitmap();

			ImageSource wpfBitmap =
				Imaging.CreateBitmapSourceFromHBitmap(
					hBitmap, IntPtr.Zero, Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions() );
			this.Icon = wpfBitmap;

			new Thread(() => {
				Logger.GetLogger("").info("Logging Loaded");
				log = Logger.GetLogger(GetType().FullName);
				try {
					//log.level = Level.SEVERE;
					int defPort = 12314;
					try {
						string port = ConfigurationManager.AppSettings[ "portNumber" ];
						if( port != null ) {
							defPort = int.Parse(port);
						}
					} catch( Exception ex ) {
						log.severe(ex);
					}

					disp.Invoke(DispatcherPriority.Normal,
						new Action(() => {
							tcpPort.Text = "" + defPort;
							log.info("starting up");
							theEventList = eventList;
							eventList.Background = Brushes.LightGray;
							addMatch.IsEnabled = false;
							FillColorsSelection();
							colorChoice.SelectedIndex = 0;
							SetMoveUpDown();
							string title = ConfigurationManager.AppSettings[ "title" ];
							if( title != null )
								Title = title;
							//			Style noSpaceStyle = new Style(typeof(Paragraph));
							//			noSpaceStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0)));
							//			eventList.Resources.Add(typeof(Paragraph), noSpaceStyle);
						}));
				} catch( Exception ex ) {
					log.severe(ex);
				}
			}).Start();
		}

		private volatile bool cancelled;

		private Dispatcher disp = Dispatcher.CurrentDispatcher;
		private void Connect_Cancel( object sender, RoutedEventArgs e ) {
			cancelled = true;
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					Connect.Content = "Connect";
					Connect.Click -= Connect_Click;
					Connect.Click -= Connect_Cancel;
					Connect.Click += Connect_Click;
					eventList.Background = Brushes.LightGray;
					//			eventList.IsEnabled = false;
					try {
						conn.Close();
					} catch( Exception ex ) {
						log.severe(ex);
					}
				})
			);
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
					eventList.Background = Brushes.Yellow;
				})
			);

			try {
				conn = new TcpClient();
				conn.SendTimeout = 5000;
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						String str = String.Format("------- Connecting to localhost:{0} at {1}...",
							port, DateTime.Now.ToLocalTime());
						AppendText(str);
					}));
				conn.BeginConnect("localhost", port, onConnect, conn);
			} catch( Exception ex ) {
				log.severe(ex);
				void start() {
					disp.Invoke( DispatcherPriority.Normal,
						new Action( () => {
							String str;
							Connect.IsEnabled = true;
							MessageBox.Show( str = String.Format( "Error Connecting to localhost:{0}\n\n{1}",
								tcpPort.Text, ex.Message ),
								String.Format( "Error Connecting to localhost:{0}", tcpPort.Text ),
								MessageBoxButton.OK, MessageBoxImage.Warning );
							AppendText( str );
						} ) );
				}
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
						AppendMoreText(str);
					}));
				log.info("Connected to " + conn);
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						Connect.Content = "Close";
						Connect.Click -= Connect_Click;
						Connect.Click -= Connect_Cancel;
						Connect.Click += Connect_Cancel;
						eventList.Background = Brushes.White;
					})
				);
				Thread th = new Thread(new ThreadStart(() => {
					try {
						log.fine("starting ReceiveData processing");
						using( conn ) {
							ReceiveData();
						}
					} catch( Exception ex ) {
						log.severe(ex);
						if( !cancelled ) {
							disp.Invoke(DispatcherPriority.Normal,
								new Action(() => {
									Connect.IsEnabled = true;
									Connect.Content = "Cancel";
									Connect.Click -= Connect_Click;
									Connect.Click -= Connect_Cancel;
									Connect.Click += Connect_Cancel;
									eventList.Background = Brushes.Yellow;
								})
							);
							Connect_Click(null, null);
						} else {
							disp.Invoke(DispatcherPriority.Normal,
								new Action(() => {
									Connect.IsEnabled = true;
									Connect.Content = "Connect";
									Connect.Click -= Connect_Click;
									Connect.Click -= Connect_Cancel;
									Connect.Click += Connect_Click;
									eventList.Background = Brushes.LightGray;
								})
							);
						}
					}
				}));
				th.IsBackground = true;
				th.Start();
			} else {
				disp.Invoke(DispatcherPriority.Normal,
					new Action(() => {
						string str = String.Format("Failed");
						AppendMoreText(str);
					}));
				if( !cancelled ) {
					try {
						conn.Close();
					} catch { }
					Thread.Sleep(500);
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
							eventList.Background = Brushes.LightGray;
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

					disp.Invoke(DispatcherPriority.Normal,
						new Action(() => {
							AppendText(s);
						}));
					
				} catch( Exception ex ) {
					log.severe(ex);
				}
			}
	
		}

		private void SelectEntireList() {
			selectionSource = eventList;
			eventList.SelectAll();
		}

		private void CopyListToClipboard() {
			try {
				StringBuilder sb = new StringBuilder();
				foreach( object row in eventList.SelectedItems ) {
					sb.Append(row.ToString());
					sb.AppendLine();
				}
				Clipboard.SetData( System.Windows.DataFormats.Text, sb.ToString());
			} catch( Exception ex ) {
				MessageBox.Show(ex.Message);
			}
		}

		private void PostAppend() {

			// keep from trimming for garbage values
			int maxLines;

			try {
				maxLines = int.Parse(trimLineCount.Text);
			} catch( Exception ex ) {
				log.fine(ex);
				return;
			}
            while (trimmingLines && eventList.Items.Count > maxLines)
            {
                MatchPatternItem mp = (MatchPatternItem) eventList.Items[0];
                cache.Add(mp);
                eventList.Items.RemoveAt(0);
            }
            while (trimmingLines && matchedList.Items.Count > maxLines)
            {
                MatchPatternItem mp = (MatchPatternItem) matchedList.Items[0];
                cache.Add(mp);
                matchedList.Items.RemoveAt(0);
            }

			if( scrollEnd ) {
                if( matchedList.Items.Count > 0 )
                    matchedList.ScrollIntoView(matchedList.Items[matchedList.Items.Count - 1]);
				eventList.ScrollIntoView(eventList.Items[ eventList.Items.Count - 1 ]);
			}
		}

		private void AppendText( string s ) {
			string[]str = s.Split('\n');
			ColorDescription d = CheckPatterns(s);
			for( int i = 0; i < str.Length; ++i ) {
				string ss = str[i];
				if( i != str.Length - 1 || ss.Length > 0 ) {
					if( d == null ) {
						eventList.Items.Add( MatchPatternFor( ss ) );
					} else {
						eventList.Items.Add( MatchPatternFor( ss, d ) );
                        if(!matchedPaused)
                            matchedList.Items.Add(MatchPatternFor(ss, d));
					}
				}
			}
			PostAppend();
		}

		private void AppendMoreText( string s ) {
			int cnt = eventList.Items.Count;
			string[]str = s.Split('\n');
			if( eventList.Items.Count == 0 ) {
				AppendText(s);
				return;
			}
			MatchPatternItem item = (MatchPatternItem)eventList.Items[ cnt - 1 ];
			item.text += str[0];
			item.Content = item.text;
			ColorDescription d = CheckPatterns(item.text);
			if( d != null ) {
				item.Recolor(d);
			}
			for( int i = 1; i < str.Length; ++i ) {
				string ss = str[ i ];
				if( i != str.Length - 1 || ss.Length > 0 ) {
					d = CheckPatterns(ss);
					if( d == null ) {
						eventList.Items.Add( MatchPatternFor( ss ) );
					} else {
						eventList.Items.Add( MatchPatternFor( ss, d ) );
					}
				}
			}
			PostAppend();
		}


		private void clearButton_Click( object sender, RoutedEventArgs e ) {
			disp.Invoke(DispatcherPriority.Normal,
				new Action(() => {
					eventList.Items.Clear();
					//FlowDocument fd = (FlowDocument)BoxRichText.Document;
					//TextPointer start = fd.ContentStart;
					//TextPointer end = fd.ContentEnd;
					//new TextRange(start, end).Text = "";
				}));
		}

		private void trimLinesTo_Checked( object sender, RoutedEventArgs e ) {
			trimmingLines = trimLinesTo.IsChecked.Value;
		}

		private void scrollToEnd_Checked( object sender, RoutedEventArgs e ) {
			scrollEnd = scrollToEnd.IsChecked.Value;
		}

		private void eventList_KeyDown( object sender, KeyEventArgs e ) {
			log.info("key pressed {0}, mods {1}", e.Key, e.KeyboardDevice.Modifiers);
			if( ( e.KeyboardDevice.Modifiers & ModifierKeys.Control ) != 0 && e.Key == Key.C ) {
				CopyListToClipboard();
			} else if( ( e.KeyboardDevice.Modifiers & ModifierKeys.Control ) != 0 && e.Key == Key.A ) {
				SelectEntireList();
			} else if( ( e.KeyboardDevice.Modifiers & ModifierKeys.Control ) != 0 && e.Key == Key.N ) {
				eventList.SelectedIndex = -1;
			}
		}

		ColorDescription CheckPatterns( string str ) {
			for( int i = 0; i < patternList.Items.Count; ++i ) {
				MatchPatternItem mpi = (MatchPatternItem)patternList.Items[ i ];
				if( mpi.Matches(str) ) {
					return mpi.color;
				}
			}
			return null;
		}

		Control selectionSource;
		//bool mouseDown;
		int startDrag = -1;
		private void mouseMoving( object sender, MouseEventArgs e ) {
			if( e.LeftButton == MouseButtonState.Pressed && startDrag != -1 ) {
				//log.info("Dragging over events: {1}", sender, eventList.Items.IndexOf(e.Source));
				int endDrag = eventList.Items.IndexOf(e.Source);
				for( int i = 0; i < eventList.Items.Count; ++i ) {
					if( endDrag < startDrag ) {
						if( i < endDrag || i > startDrag ) {
							eventList.SelectedItems.Remove(eventList.Items[ i ]);
						} else if( i <= startDrag ) {
							eventList.SelectedItems.Add(eventList.Items[ i ]);
						}
					} else {
						if( i < startDrag || i > endDrag ) {
							eventList.SelectedItems.Remove(eventList.Items[ i ]);
						} else if( i <= endDrag ) {
							eventList.SelectedItems.Add(eventList.Items[ i ]);
						}
					}
				}	
			}
		}

		private void mouseSelected( object sender, MouseEventArgs e ) {
			log.info("Dragging Enter events: {1}", sender, eventList.Items.IndexOf(e.Source));
			log.info("mouse selected from {0}: args: {1}", sender, e);
			if( ( Keyboard.Modifiers & ModifierKeys.Shift ) != 0 ) {
				log.info("shift-click events: {1}", sender, eventList.Items.IndexOf(e.Source));
				int endDrag = eventList.Items.IndexOf(e.Source);
				for( int i = 0; i < eventList.Items.Count; ++i ) {
					if( endDrag < startDrag ) {
						if( i < endDrag || i > startDrag ) {
							eventList.SelectedItems.Remove(eventList.Items[ i ]);
						} else if( i <= startDrag ) {
							eventList.SelectedItems.Add(eventList.Items[ i ]);
						}
					} else {
						if( i < startDrag || i > endDrag ) {
							eventList.SelectedItems.Remove(eventList.Items[ i ]);
						} else if( i <= endDrag ) {
							eventList.SelectedItems.Add(eventList.Items[ i ]);
						}
					}
				}
			} else {
				log.info("simple click down, adding {0}", e.Source);
				startDrag = eventList.Items.IndexOf(e.Source);
//				eventList.SelectedItems.Clear();
	//			eventList.SelectedItem = null;
				eventList.SelectedIndex = startDrag;
			}
		}

		private void mouseDeselected( object sender, MouseEventArgs e ) {
			//log.info("Dragging leave events: {1}", sender, eventList.Items.IndexOf(e.Source));
			startDrag = -1;
		}

		private bool IsBright( Brush brush ) {
			bool isBright = false;
			if( brush is SolidColorBrush ) {
				SolidColorBrush br = brush as SolidColorBrush;
				int r = br.Color.R;
				int g = br.Color.G;
				int b = br.Color.B;
				isBright = ( r + g + b ) > 200;
				Console.WriteLine( "r={0}, g={1}, b={2} = {3}", r, g, b, r + g + b );
			} else {
				Console.WriteLine( "Unknown brush type: {0}", brush.GetType().Name );
			}

			return isBright;
		}

		private void eventList_SelectionChanged( object sender, SelectionChangedEventArgs e ) {
			Console.WriteLine( "selection changed" );
			foreach( MatchPatternItem el in e.AddedItems ) {
				Console.WriteLine( "Setting dark foreground for: {0}", el );
				el.Foreground = /* IsBright( el.color.back ) ? SystemColors.ControlLightLightBrush : */ SystemColors.ControlDarkDarkBrush;
			}
			foreach( MatchPatternItem el in e.RemovedItems ) {
				Console.WriteLine( "Resetting dark foreground for: {0}", el );
				el.Foreground = el.color.fore;
			}
			//if( (e.AddedItems.Count == 1 || e.RemovedItems.Count == 1) && selectionSource == null) {
			//	log.info("1 selected, source: {0}", e.Source);
			//	foreach( object o in e.AddedItems ) {
			//		eventList.SelectedItem = o;
			//		break;
			//	}
			//}
			//selectionSource = null;
		}

		private void patternList_SelectionChanged( object sender, SelectionChangedEventArgs e ) {
			if( patternList.SelectedIndex >= 0 ) {
				editMatch.IsEnabled = deleteMatch.IsEnabled = patternList.Items.Count > 0 && patternList.SelectedItems.Count == 1;
				SetMoveUpDown();
				MatchPatternItem item = (MatchPatternItem)patternList.Items[ patternList.SelectedIndex ];
				editing = item;
				matchPattern.Text = item.text;
				colorChoice.SelectedIndex = item.index;
				addMatch.Content = "Add";
				//for( int i = 0; i < patternList.Items.Count; ++i ) {
				//	if( i != patternList.SelectedIndex ) {
				//		MatchPatternItem mpi = (MatchPatternItem)patternList.Items[ i ];
				//		mpi.Background = item.color.back;
				//		mpi.Foreground = item.color.fore;
				//	}
				//}
				//item.Background = item.color.fore;
				//item.Foreground = item.color.back;
			} else {
				editMatch.IsEnabled = deleteMatch.IsEnabled = false;
			}
		}

		private class ColorDescription {
			public  Brush back;
			public  Brush fore;
			public  string name;
			public ColorDescription( Brush back, Brush fore, string name ) {
				this.back = back;
				this.fore = fore;
				this.name = name;
			}
			public override string ToString() {
				return this.name+":("+fore.ToString()+")("+back.ToString()+")";
			}
		}

		private static ColorDescription[] colors = {
			new ColorDescription( Brushes.Red, Brushes.White, "Red"),
 			new ColorDescription( Brushes.Yellow, Brushes.Black, "Yellow"),
			new ColorDescription( Brushes.Blue, Brushes.White, "Blue"),
			new ColorDescription( Brushes.Purple, Brushes.White, "Purple"),
			new ColorDescription( Brushes.Orange, Brushes.Black, "Orange"),
			new ColorDescription( Brushes.Gray, Brushes.White, "Gray"),
			new ColorDescription( Brushes.Green, Brushes.White, "Green"),
			new ColorDescription( Brushes.Cyan, Brushes.Black, "Cyan"),
			new ColorDescription( Brushes.DarkBlue, Brushes.White, "Dark Blue"),
			new ColorDescription( Brushes.DarkCyan, Brushes.White, "Dark Cyan"),
			null
		};

		private class PatternItem: ComboBoxItem {
			public  string text;
			public  ColorDescription color;
			public  int index;
			public PatternItem( string text, ColorDescription color, int index, Control control ) {
				this.text = text;
				this.Content = text;
				this.index = index;
				this.color = color;
				Background = color.back;
				Foreground = color.fore;
				this.Margin = new Thickness(0, 0, 0, 0);
				this.Width = control.Width;
			}
			public override string ToString() {
				return text;
			}
		}

		private static ColorDescription blackAndWhite = new ColorDescription(Brushes.Transparent, Brushes.Black, "White");
		private MatchPatternItem editing;
		List<MatchPatternItem> cache = new List<MatchPatternItem>();

		private object MatchPatternFor( string ss, ColorDescription color ) {
			MatchPatternItem mp;
			if( cache.Count > 0 ) {
				mp = cache[ cache.Count - 1 ];
				cache.RemoveAt( cache.Count - 1 );
				mp.Content = mp.text = ss;
				mp.color = color;
				mp.Background = color.back;
				mp.Foreground = color.fore;
			} else {
				mp = new MatchPatternItem( ss, color );
			}
			return mp;
		}

		private object MatchPatternFor( string ss ) {
			MatchPatternItem mp;
			if( cache.Count > 0 ) {
				mp = cache[ cache.Count - 1 ];
				cache.RemoveAt( cache.Count - 1 );
				mp.Content = mp.text = ss;
				mp.color = blackAndWhite;
				mp.Background = blackAndWhite.back;
				mp.Foreground = blackAndWhite.fore;
			} else {
				mp = new MatchPatternItem( ss, blackAndWhite );
			}
			return mp;
		}

		private object MatchPatternFor( string text, ColorDescription color, int index, ListBox patternList ) {
			MatchPatternItem mp;
			if( cache.Count > 0 ) {
				mp = cache[ cache.Count - 1 ];
				cache.RemoveAt( cache.Count - 1 );
				mp.text = text;
				mp.Content = text;
				mp.index = index;
				mp.color = color;
				mp.Background = color.back;
				mp.Foreground = color.fore;
				mp.Margin = new Thickness( 0, 0, 0, 0 );
				mp.Width = patternList.Width;
			} else {
				mp = new MatchPatternItem( text, color, index, patternList );
			}
			return mp;
		}

		private class MatchPatternItem: ListBoxItem, IDisposable {
			public  string text;
			public  ColorDescription color;
			public  int index;

			public void Dispose() {
				this.color = null;
				this.Content = null;
				this.text = null;
				base.Resources.Clear();
			}

			public MatchPatternItem( string text, ColorDescription color, int index, Control control ) {
				this.text = text;
				this.Content = text;
				this.index = index;
				this.color = color;
				Background = color.back;
				Foreground = color.fore;
				this.Margin = new Thickness(0, 0, 0, 0);
				this.Width = control.Width;
			}
			protected override void OnUnselected( RoutedEventArgs e ) {
				base.OnUnselected(e);
				Foreground = color.fore;
				Background = color.back;
			}
			protected override void OnSelected( RoutedEventArgs e ) {
				base.OnSelected(e);

				if( color == blackAndWhite ) {
					Foreground = Brushes.White;
					Style s = new Style(typeof(ListBox));
					if( theEventList != null )
						theEventList.Style = s;
				} else {
					Foreground = color.back;
					Style s = new Style(typeof(ListBox));
					s.Resources.Add(SystemColors.HighlightBrushKey, color.fore);
					if( theEventList != null )
						theEventList.Style = s;
				}
			}

			public MatchPatternItem( string ss ) : this( ss, blackAndWhite ) {
			}

			public MatchPatternItem( string ss, ColorDescription d ) {
				// TODO: Complete member initialization
				this.Content = this.text = ss;
				this.color = d;
				Background = color.back;
				Foreground = color.fore;
			}

			public override string ToString() {
				return text;
			}

			internal bool Matches( string str ) {
				Regex exp = new Regex(this.text);
				return( exp.Matches(str).Count > 0 ) ;
				//return str.Contains(this.text);
			}

			internal void Recolor( ColorDescription d ) {
				this.color = d;
				this.Foreground = d.fore;
				this.Background = d.back;
			}
		}

		private void FillColorsSelection() {
			for( int i = 0; colors[ i ] != null; i ++ ) {
				PatternItem item = new PatternItem(colors[ i ].name, colors[ i ], i, colorChoice);
				colorChoice.Items.Add(item);
			}
		}

		private void addMatch_Click( object sender, RoutedEventArgs e ) {
			if( addMatch.Content.Equals("Save") ) {
				editing.Content = editing.text = matchPattern.Text;
				ColorDescription d = colors[ colorChoice.SelectedIndex ];
				editing.Foreground = d.fore;
				editing.Background = d.back;
				editing.color = d;
				addMatch.Content = "Add";
				editMatch.IsEnabled = deleteMatch.IsEnabled = patternList.SelectedIndex != -1;
				SetMoveUpDown();
				matchPattern.Text = "";
				dirty = true;
			} else {
				patternList.Items.Add(MatchPatternFor(matchPattern.Text, colors[ colorChoice.SelectedIndex ], colorChoice.SelectedIndex, patternList));
				matchPattern.Text = "";
				dirty = true;
			}
		}


		private void editMatch_Click( object sender, RoutedEventArgs e ) {
			//patternList.Items[ patternList.SelectedIndex ] = MatchPatternFor(matchPattern.Text, colors[ colorChoice.SelectedIndex ], colorChoice.SelectedIndex, colorChoice); 
			addMatch.Content = "Save";
			deleteMatch.IsEnabled = false;
			editMatch.IsEnabled = false;
			SetMoveUpDown();
//			editing = (MatchPatternItem)patternList.Items[ patternList.SelectedIndex ];
		}

		private void deleteMatch_Click( object sender, RoutedEventArgs e ) {
			deleteMatch.IsEnabled = false;
			if( patternList.SelectedIndex >= 0 && patternList.SelectedIndex < patternList.Items.Count ) {
				patternList.Items.RemoveAt( patternList.SelectedIndex );
			}
			dirty = true;
		}

		private void matchPattern_TextChanged( object sender, TextChangedEventArgs e ) {
			addMatch.IsEnabled = matchPattern.Text.Length > 0;
		}

		private void colorChoice_SelectionChanged( object sender, SelectionChangedEventArgs e ) {
			PatternItem pa = (PatternItem)colorChoice.SelectedItem;
//			colorChoice.Foreground = pa.Foreground;
			colorChoice.Background = pa.Background;
			
		}

		internal void SetMoveUpDown() {
			moveUp.IsEnabled = patternList.SelectedIndex > 0;
			moveDown.IsEnabled = patternList.SelectedIndex < patternList.Items.Count - 1;
		}

		private void moveDown_Click( object sender, RoutedEventArgs e ) {
			int idx = patternList.SelectedIndex;
			MatchPatternItem mpi = (MatchPatternItem)patternList.Items[ idx ];
			patternList.Items.RemoveAt(idx);
			patternList.Items.Insert(idx+1, mpi);
			patternList.SelectedIndex = idx + 1;
			dirty = true;
			SetMoveUpDown();
		}

		private void moveUp_Click( object sender, RoutedEventArgs e ) {
			int idx = patternList.SelectedIndex;
			MatchPatternItem mpi = (MatchPatternItem)patternList.Items[ idx ];
			patternList.Items.RemoveAt(idx);
			patternList.Items.Insert(idx - 1, mpi);
			patternList.SelectedIndex = idx - 1;
			dirty = true;
			SetMoveUpDown();
		}

		private void rematchPatterns_Click( object sender, RoutedEventArgs e ) {
			for( int i = 0; i < eventList.Items.Count; ++i ) {
				try {
					MatchPatternItem mpi = (MatchPatternItem)eventList.Items[ i ];
					ColorDescription d = CheckPatterns(mpi.text);
					if( d != null ) {
						mpi.Recolor(d);
					}
				} catch( Exception ex ) {
					log.severe(ex);
					break;
				}
			}
		}

		string lastdir=null, lastFile="Matches";
		private void FileMenu_Click( object sender, RoutedEventArgs e ) {
			log.info("FileMenu Click: {0}, {1}", sender, e);
			FileDialog d;
			bool? success;
			if( e.Source == savePatterns ) {
				if( lastFile != null ) {
					WritePatternsTo(lastFile);
					dirty = false;
					return;
				}
				d = new SaveFileDialog();
				d.Title = "Save Patterns";
				d.AddExtension = true;
				d.CheckFileExists = false;
				d.FileName = "MatchPattern";
				d.DefaultExt = "mpat";
				d.Filter = "Patterns (*.mpat)|*.mpat|All Files (*.*)|*.*";
				d.InitialDirectory = lastdir ?? Directory.GetCurrentDirectory();
				success = d.ShowDialog();
				if( success.Value ) {
					string name = d.FileName;
					log.info("Selected {0}", name);
					lastdir = new DirectoryInfo(name).Parent.FullName.ToString();
					log.info("Saving dir {0}", lastdir);
					WritePatternsTo(lastFile=name);
					dirty = false;

				} else {
					log.info("Save was cancelled");
				}
			} else if( e.Source == saveAsPatterns ) {
				d = new SaveFileDialog();
				d.Title = "Save Patterns As...";
				d.AddExtension = true;
				d.CheckFileExists = false;
				d.FileName = lastFile;
				d.DefaultExt = "mpat";
				d.Filter = "Patterns (*.mpat)|*.mpat|All Files (*.*)|*.*";
				d.InitialDirectory = lastdir ?? Directory.GetCurrentDirectory();
				success = d.ShowDialog();
				if( success.Value ) {
					string name = d.FileName;
					log.info("Selected {0}", name);
					lastdir = new DirectoryInfo(name).Parent.FullName.ToString();
					log.info("Saving dir {0}", lastdir);
					WritePatternsTo(lastFile = name);
					dirty = false;
				} else {
					log.info("Save was cancelled");
				}
			} else if( e.Source == loadPatterns ) {
				if( dirty ) {
					MessageBoxResult res = MessageBox.Show(this, "Current Patterns Have Not Been Saved, Discard?", "Discard Unsaved Patterns?", MessageBoxButton.YesNoCancel);
					if( res == MessageBoxResult.Cancel ) {
						return;
					} else if( res == MessageBoxResult.No ) {
						if( lastFile != null ) {
							WritePatternsTo(lastFile);
							dirty = false;
						} else {
							d = new SaveFileDialog();
							d.Title = "Save Patterns As...";
							d.AddExtension = true;
							d.CheckFileExists = false;
							d.FileName = lastFile;
							d.DefaultExt = "mpat";
							d.Filter = "Patterns (*.mpat)|*.mpat|All Files (*.*)|*.*";
							d.InitialDirectory = lastdir ?? Directory.GetCurrentDirectory();
							success = d.ShowDialog();
							if( success.Value ) {
								string name = d.FileName;
								log.info("Selected {0}", name);
								lastdir = new DirectoryInfo(name).Parent.FullName.ToString();
								log.info("Saving dir {0}", lastdir);
								WritePatternsTo(lastFile = name);
								dirty = false;
							} else {
								log.info("Save was cancelled");
								return;
							}
						}
					}
				}
				d = new OpenFileDialog();
				d.Title = "Load Patterns";
				d.AddExtension = true;
				d.CheckFileExists = true;
				d.FileName = "MatchPattern";
				d.DefaultExt = "mpat";
				d.Filter = "Patterns (*.mpat)|*.mpat|All Files (*.*)|*.*";
				d.InitialDirectory = lastdir ?? Directory.GetCurrentDirectory();
				success = d.ShowDialog();
				if( success.Value ) {
					string name = d.FileName;
					log.info("Selected {0}", name);
					lastdir = new DirectoryInfo(name).Parent.FullName.ToString();
					log.info("Saving dir {0}", lastdir);
					LoadPatternsFrom(lastFile = name);
					dirty = false;
				} else {
					log.info("Save was cancelled");
				}
			}
		}

		private void WritePatternsTo( string p ) {
			List<string>lines = new List<string>();
			foreach( MatchPatternItem mi in patternList.Items ) {
				lines.Add(mi.text);
				lines.Add(mi.color.name);
				log.info("saved pattern \"{0}\" : {1}", mi.text, mi.color.name);
			}
			File.WriteAllLines(p, lines.ToArray());
		}

		private void LoadPatternsFrom( string p ) {
			string[] arr = File.ReadAllLines(p);
			patternList.Items.Clear();
			for( int i = 0; i < arr.Length; i += 2 ) {
				try {
					string text = arr[ i ];
					string color = arr[ i + 1 ];
					ColorDescription d = null;
					foreach( PatternItem cd in colorChoice.Items ) {
						if( cd.color.name.Equals(color) ) {
							d = cd.color;
							break;
						}
					}
					if( d == null ) {
						d = ((PatternItem)colorChoice.Items[ 0 ]).color;
					}
					patternList.Items.Add( MatchPatternFor( text, d ) );
				} catch( Exception ex ) {
					log.severe(ex);
					MessageBox.Show(ex.Message);
				}
			}
		}


		private void EditMenu_Click( object sender, RoutedEventArgs e ) {
			log.info("EditMenu Click: {0}, {1}", sender, e.Source);
			if( e.Source == selectAll ) {
				SelectEntireList();
			} else if( e.Source == selectNone ) {
				eventList.SelectedIndex = -1;
			} else if( e.Source == copySelection ) {
				CopyListToClipboard();
			}
		}

		private void findMatchPattern_Click( object sender, RoutedEventArgs e ) {
			Regex r = new Regex( searchBox.Text );
			eventList.SelectedIndex = -1;
			foreach( MatchPatternItem line in eventList.Items ) {
				if( r.Matches(line.text).Count > 0 ) {
					eventList.SelectedItems.Add(line);
				}
			}
		}

		private void searchBox_SearchSelected( object sender, MouseEventArgs e ) {
			//if( searchBox.SelectionLength == 0 ) {
			//	searchBox.SelectionStart = 0;
			//	searchBox.SelectionLength = searchBox.Text.Length;
			//}
		}

		private void searchBox_TextChanged( object sender, TextChangedEventArgs e ) {

		}

		private void FontMenu_Click( object sender, RoutedEventArgs e ) {
			MenuItem itm = (MenuItem)e.Source;
			log.info( "Font select: {0}", itm.Name );
			MenuItem fonts = (MenuItem)sender;
			foreach( MenuItem i in fonts.Items ) {
				i.IsChecked = false;
			}
			itm.IsChecked = true;
			if( itm.Name.Equals( "smallFont" ) ) {
				eventList.FontSize = 10;
			} else if( itm.Name.Equals( "mediumFont" ) ) {
				eventList.FontSize = 13;
			} else if( itm.Name.Equals( "largeFont" ) ) {
				eventList.FontSize = 16;
			} else if( itm.Name.Equals( "exlargeFont" ) ) {
				eventList.FontSize = 19;
			}  
		}
        
        bool matchedPaused;
        private void PauseMatchesButton_Click(object sender, RoutedEventArgs e)
        {
            matchedPaused = PauseMatchesButton.IsChecked.Value;
        }

        private void ClearMatchesButton_Click(object sender, RoutedEventArgs e)
        {
            matchedList.Items.Clear();
        }
	}
	public class MyLogger {
		Handler h;
		public Level level { get; set; }
		public MyLogger( string name ) {
			h = new ConsoleHandler();
			//h = new FileHandler("C:/temp2/log");
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
