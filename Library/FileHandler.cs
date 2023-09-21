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
		private bool firstStart = false;
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
				while( !processor() ) {
					lock( this ) {
						Monitor.Wait(this, 400);
					}
				}
				if( outf != null ) {
					outf.Close();
					outf = null;
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

				try {
					outp = new StreamWriter( rname + (gens>1 ? ".0" : ""), append );
					outp.AutoFlush = false;
					finfo = new FileInfo( rname+ (gens>1 ? ".0" : "") );
				} catch( DirectoryNotFoundException ex ) {
					LogManager.ConsoleWriteLine("Error opening log file (name=\"" + rname + "\") for writing: " + ex.Message + "\n" + ex.StackTrace, typeof(FileHandler).FullName);
					Exception ee = ex.InnerException;
					while( ee != null ) {
						LogManager.ConsoleWriteLine("InnerException: " + ee.Message + "\n" + ee.StackTrace, typeof(FileHandler).FullName);
						ee = ee.InnerException;
					}
					new FileInfo(rname).Directory.Create();
				} catch( IOException ex ) {

					LogManager.ConsoleWriteLine("Error opening log file (name=\"" + rname + "\") for writing: " + ex.Message + "\n" + ex.StackTrace, typeof(FileHandler).FullName);
					Exception ee = ex.InnerException;
					while( ee != null ) {
						LogManager.ConsoleWriteLine("InnerException: " + ee.Message + "\n" + ee.StackTrace, typeof(FileHandler).FullName);
						ee = ee.InnerException;
					}
					rname = name + "-" + cnt++;
					LogManager.ConsoleWriteLine("Trying file: \"" + rname + "\" next", typeof(FileHandler).FullName);
				}
			} while( outp == null );
			name = rname;
			return outp;
		}

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
			if( asyncFlush && outf != null )
				outf.Flush();
		}

		protected virtual bool CheckNewFile() {
			return( outf == null || new FileInfo( name+(gens>1 ? ".0" : "") ).Length > limit );
		}

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

			if ( outf == null || CheckNewFile( ) ) {
				Close();
				shuffleDown( );
				// trunc/reopen the file.
				outf = baseFileOpen( false );
				len = 0;
			}

			rec.SequenceNumber = NextSequence;
			String str = Formatter.format( rec );
			try {
				if( outf != null ) {
					outf.Write( str );
					if( !asyncFlush )
						outf.Flush();
				} else {
					Console.Write( "# NOFILE # "+str );
				}
			} catch( IOException ex ) {
				Console.Write("# SEVERE # Error writting to log file: "+ex.Message+"\n# SEVERE #"+str );
			}
			len = finfo.Length;
		}
	}
}
