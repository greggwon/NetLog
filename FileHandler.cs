using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace NetLog.Logging
{
	public class FileHandler : Handler
	{
//		private Logger log = Logger.GetLogger("NetLog.Logging.FileHandler");
		private StreamWriter outf;
		private long len, limit;
		private int gens;
		protected FileInfo finfo;
		private string name;
		private volatile bool firstStart = false;
		private bool asyncFlush = true;
		private bool consoleDebug;

		public new bool ConsoleDebug {
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		public string Filename {
			get { return Name; }
			set {
				Name = value;
			}	
		}

		public override void Close() {
			try {
				lock( this ) {
					while( !processor() ) {
						lock( this ) {
							Monitor.Wait(this, 400);
						}
					}
					if( outf != null ) {
						try {
							outf.Close();
						} finally {
							outf = null;
						}
					}
				}
			} catch( Exception ex ) {
				LogManager.ReportExceptionToEventLog("error closing FileHandler", ex);
			}
		}

		public override void Flush() {
			while ( !processor( ) ) {
				if ( consoleDebug )
					Console.WriteLine( "still output pending, waiting" );
				lock ( this ) {
					Monitor.Wait( this, 400 );
				}
			}
			if ( consoleDebug )
				Console.WriteLine( "no more output pending, flushing I/O" );
			if( outf != null ) {
				outf.Flush( );
			}
		}

		public bool IncrementalFlush {
			get { return asyncFlush; }
			set { asyncFlush = value; }
		}

		public int Generations {
			get { return gens; }
			set { gens = value; }
		}

		public long Limit {
			get { return limit; }
			set { limit = value; }
		}

		public string Name {
			get { return name; }
			set {
				if( this.name != null && this.name.Equals( value ) )
					return;
				if( value.StartsWith("/") || value.StartsWith("\\") || value[ 1 ] == ':' ) {
					this.name = value;
				} else {
					// add the current directory path
					this.name = Path.Combine(Directory.GetCurrentDirectory(), value);
				}
				lock( this ) {
					if( outf != null ) {
						Close();
						outf = baseFileOpen(true);
					}
				}
			}
		}

		public virtual string baseFileName( string path ) {
			if( gens > 1 )
				return path+".0";
			return path;
		}

		public FileHandler( string name, int generations, long size ) {
			limit = size;
			gens = generations;
			Name = name;
			FileInfo f = new FileInfo( baseFileName( name ) );
			len = 0;
			if( f.Exists )
				len = f.Length;
			WaitCount = 100;
			outf = baseFileOpen( true );
		}

		public FileHandler(string name)
			: this(name, 5, 20 * 1024 * 1024)
		{
			Formatter = new StreamFormatter( false, true, false );
		}

		public FileHandler() : this( "logging.out", 5, 20 *1024*1024 ) {
			Formatter = new StreamFormatter( false, true, false );
		}

		protected virtual void shuffleDown() {
			string t1 = this.name + ".temp";
			for (int i = gens - 1; i > 0; --i)
			{
				string f1 = this.name+"."+i;
				FileInfo f2 = new FileInfo( this.name+"."+(i-1) );
				FileInfo fi = new FileInfo(f1);
				try {
				    if( f2.Exists && f2.Length > 0 ) {
						if( consoleDebug )
							Console.WriteLine("replace "+f1+" with "+f2 );
						// truncate f1 and/or make it exist so we can replace to it.
						File.Create( f1 ).Close();
						// Copy i-1 file to i file, removing i-1 file.
						f2.Replace( f1, t1 );
						if (consoleDebug)
							Console.WriteLine("f1 exists: " + new FileInfo(f1).Exists + ", f2 exists: " + f2.Exists);
					} else {
						if (consoleDebug)
							Console.WriteLine("there is no " + f2 + " skipping rename");
						if( fi.Exists && fi.Length > 0 )
							fi.Delete();
					}
				} catch( Exception ex ) {
//					log.log( Level.SEVERE, ex );
					Console.WriteLine( "# SEVERE # Exception rotating files: " + ex.Message + "\n" + ex.StackTrace );
				}
			}
			if( new FileInfo(t1).Exists )
				new FileInfo( t1 ).Delete();
		}

		protected virtual StreamWriter baseFileOpen( bool append) {
			string rname = name;
			StreamWriter outp = null;
			int cnt = 1;
			FileInfo fi = new FileInfo(rname);
			bool explicitDir = false;
			if( rname.ToUpper().Replace('/', '\\').StartsWith(fi.Directory.Root.Name.ToUpper()) ) {
				explicitDir = true;
			}
			if( !explicitDir) {
				string newrname = "";
				try {
					string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					string file = GetType().Assembly.Location;
					string app = System.IO.Path.GetFileNameWithoutExtension(file);
					newrname = appData + "\\" + app + "\\" + rname;
					new FileInfo(newrname).Directory.Create();
					rname = newrname;
				} catch( Exception ex ) {
					LogManager.ReportExceptionToEventLog(this.GetType().FullName+": Can not log to directory \"" + newrname + "\": " + ex.Message, ex);
				}
			}

			do {
				String path = rname + (gens > 1 ? ".0" : "");
				LogManager.ConsoleWriteLine( "Reopening: " + path );
				try {
					outp = new StreamWriter( path, append );
					outp.AutoFlush = false;
					finfo = new FileInfo( path );
				} catch( DirectoryNotFoundException ex ) {
					LogManager.ConsoleWriteLine("Error opening log file (name=\"" + rname + "\") for writing: " + ex.Message + "\n" + ex.StackTrace, typeof(FileHandler).FullName);
					LogManager.ReportExceptionToEventLog( "Error opening "+path+": "+ex.Message, ex );
					try {
						new FileInfo( rname ).Directory.Create();
					} catch( Exception dex ) {
						LogManager.ReportExceptionToEventLog( "Error creating directory to " + path + ": " + dex.Message, dex );
					}
				} catch( Exception ex ) {
                    // This exception can happen for either out of space, file locked, or file not accessible etc.
                    // If a process is ran as one user, and creates log files, and then is run as another
                    // user in the same directory, with the same logging location, the process, at startup,
                    // may not be able to rotate the log files to create a new .0 file.  The handling here,
                    // as with other exceptions which deny access to logging, will create a new name for the
                    // log file to try and find something that works.

					LogManager.ConsoleWriteLine("Error opening log file (name=\"" + rname + "\") for writing: " + ex.Message + "\n" + ex.StackTrace, typeof(FileHandler).FullName);
					LogManager.ReportExceptionToEventLog( "Error creating directory to " + path + ": " + ex.Message, ex );
					rname = name + "-" + cnt++;

#if Switch_To_Console_After_Some_Failures
                    // There is still more work to do on this to handle (outf == null) checks elsewhere, and then
                    // the friendly thing to do, which is to keep retrying occassionally to switch back to file logging
                    // in case some admin task fixes the issue and makes the location writeable.
                    if (cnt > 10)
                    {
                        // Switch to writing to Console.Out
                        outp = new ConsoleWriter(Console.Out);
                    }
                    else
#endif
                    {
                        LogManager.ConsoleWriteLine("Trying file: \"" + rname + "\" next", typeof(FileHandler).FullName);
                    }
				}
			} while( outp == null );
			name = rname;
			return outp;
		}

		private StringBuilder bld = new StringBuilder();
		public override void Publish( LogRecord rec ) {
			if( !firstStart ) {
				lock( this ) {
					if( !firstStart ) {
						Close();
						shuffleDown();
						outf = baseFileOpen(false);
						firstStart = true;
					}
				}
			}
			Enqueue( rec );
		}

		protected override void PushBoundry() {
		
			if( outf == null || CheckNewFile() ) {
				Close();
				shuffleDown();
				// trunc/reopen the file.
				outf = baseFileOpen( false );
				len = 0;
			}

			outf.Write( bld.ToString() );
			bld.Clear();
			if( asyncFlush && outf != null )
				outf.Flush();
			len = finfo.Length;
		}

		protected virtual bool CheckNewFile() {
			return( outf == null || new FileInfo( name+(gens>1 ? ".0" : "") ).Length > limit );
		}

		private SpinLock pushLock = new SpinLock();

		/// <summary>
		/// There will only ever be exactly one thread calling into this method, so
		/// we don't need to lock anything regarding output file changes.  The single
		/// thread will see an atomic view of the changes it is making and that keeps
		/// us from having to worry about multiple open/closes or file rotations
		/// </summary>
		/// <param name="rec"></param>
		protected override void Push( LogRecord rec ) {
			// stop now if not loggable
			if (rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF)
			{
				return;
			}

			rec.SequenceNumber = NextSequence;
			String str = Formatter.format( rec );
			try {
				if( outf != null ) {
					bld.Append( str );
				} else {
					Console.Write( "# NOFILE # "+str );
				}
			} catch( IOException ex ) {
				Console.Write("# SEVERE # Error writting to log file: "+ex.Message+"\n# SEVERE #"+str );
			}
		}
	}

#if Switch_To_Console_After_Some_Failures
    /// <summary>
    /// A StreamWriter to write the Console.out TextWriter if no output file seems to be writable.
    /// There is still more work to do on this to handle (outf == null) checks elsewhere, and then
    /// the friendly thing to do, which is to keep retrying occassionally to switch back to file logging
    /// in case some admin task fixes the issue and makes the location writeable.
    /// </summary>
    private class ConsoleWriter : StreamWriter
    {
        private TextWriter textWriter;

        public ConsoleWriter(TextWriter textWriter)
        {
            // TODO: Complete member initialization
            this.textWriter = textWriter;

        }
        public override void Write(bool value)
        {
            textWriter.Write(value);
        }
        public override void Write(char value)
        {
            textWriter.Write(value);
        }
        public override void Write(char[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            textWriter.Write(buffer, index, count);
        }
        public override void Write(decimal value)
        {
            textWriter.Write(value);
        }

        public override void Write(double value)
        {
            textWriter.Write(value);
        }
        public override void Write(float value)
        {
            textWriter.Write(value);
        }
        public override void Write(int value)
        {
            textWriter.Write(value);
        }
        public override void Write(long value)
        {
            textWriter.Write(value);
        }
        public override void Write(object value)
        {
            textWriter.Write(value);
        }
        public override void Write(string format, object arg0)
        {
            textWriter.Write(format, arg0);
        }
        public override void Write(string format, object arg0, object arg1)
        {
            textWriter.Write(format, arg0, arg1);
        }
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            textWriter.Write(format, arg0, arg1, arg2);
        }
        public override void Write(string format, params object[] arg)
        {
            textWriter.Write(format, arg);
        }
        public override void Write(string value)
        {
            textWriter.Write(value);
        }
        public override void Write(uint value)
        {
            textWriter.Write(value);
        }
        public override void Write(ulong value)
        {
            textWriter.Write(value);
        }
        public override void WriteLine(bool value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(char value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(char[] buffer)
        {
            WriteLine(buffer, 0, buffer.Length);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            textWriter.WriteLine(buffer, index, count);
        }
        public override void WriteLine(decimal value)
        {
            textWriter.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(float value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(int value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(long value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(object value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(string format, object arg0)
        {
            textWriter.WriteLine(format, arg0);
        }
        public override void WriteLine(string format, object arg0, object arg1)
        {
            textWriter.WriteLine(format, arg0, arg1);
        }
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            textWriter.WriteLine(format, arg0, arg1, arg2);
        }
        public override void WriteLine(string format, params object[] arg)
        {
            textWriter.WriteLine(format, arg);
        }
        public override void WriteLine(string value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(uint value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine(ulong value)
        {
            textWriter.WriteLine(value);
        }
        public override void WriteLine()
        {
            textWriter.WriteLine();
        }
    }
#endif
}
