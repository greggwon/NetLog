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
		private Logger log = Logger.getLogger("Seqtech.Logging.FileHandler");
		private StreamWriter outf;
		private long len, limit;
		private int gens;
		protected FileInfo finfo;
		private string name;
		private bool asyncFlush = true;
		private bool consoleDebug;

		public new bool ConsoleDebug {
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		public string Filename {
			get { return name; }
			set { 
				name = value;
				if( outf != null ) {
					Flush();
					Close();
				}
			}	
		}

		public override void Close() {
			while( !processor() ) {
				lock( this ) {
					Monitor.Wait( this, 400 );
				}
			}
			if( outf != null ) {
				outf.Close();
				outf = null;
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
			outf.Flush( );
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
				this.name = value;
				lock( this ) {
					Close();
					outf = baseFileOpen( true );
				}
			}
		}

		public string baseFileName( string path ) {
			if( gens > 1 )
				return path+".0";
			return path;
		}

		public FileHandler( string name, int generations, long size ) {
			limit = size;
			gens = generations;
			this.name = name;
			FileInfo f = new FileInfo( baseFileName( name ) );
			len = 0;
			if( f.Exists )
				len = f.Length;

			outf = baseFileOpen( true );
		}

		public FileHandler(string name)
			: this(name, 1, 20 * 1024 * 1024)
		{
			Formatter = new StreamFormatter( false, true, false );
		}

		public FileHandler() {
			Formatter = new StreamFormatter( false, true, false );
			Generations = 1;
			limit = 20 * 1024 * 1024;
		}

		protected virtual void shuffleDown() {
			string t1 = this.name + ".temp";
			for (int i = gens - 1; i > 0; --i)
			{
				string f1 = this.name+"."+i;
				FileInfo f2 = new FileInfo( this.name+"."+(i-1) );
				try {
				    if( f2.Exists ) {
						if( consoleDebug )
							Console.WriteLine("replace "+f1+" with "+f2 );
						File.Create( f1 ).Close();
						f2.Replace( f1, t1 );
						if (consoleDebug)
							Console.WriteLine("f1 exists: " + new FileInfo(f1).Exists + ", f2 exists: " + f2.Exists);
					} else {
						if (consoleDebug)
							Console.WriteLine("there is no " + f2 + " skipping rename");
					}
				} catch( Exception ex ) {
					log.log( Level.SEVERE, ex );
//					Console.WriteLine( "Exception rotating files: "+ex.Message+"\n"+ex.StackTrace );
				}
			}
			if( new FileInfo(t1).Exists )
				new FileInfo( t1 ).Delete();
		}

		protected virtual StreamWriter baseFileOpen( bool append) {
			string rname = name;
			StreamWriter outp = null;
			int cnt = 1;
			do {

				try {
					outp = new StreamWriter( rname + ".0", append );
					outp.AutoFlush = false;
					finfo = new FileInfo( rname+".0" );
				} catch( IOException ex ) {

					Console.WriteLine( "Error opening log file (name=\""+rname+"\") for writing: "+ex.Message+"\n"+ex.StackTrace );
					rname = name + "-" + cnt++;
					Console.WriteLine( "Trying file: \""+rname+"\" next");
				}
			} while( outp == null );
			name = rname;
			return outp;
		}

		public override void Publish( LogRecord rec ) {
			Enqueue( rec );
		}

		protected override void PushBoundry() {
			if( asyncFlush && outf != null )
				outf.Flush();
		}

		protected virtual bool CheckNewFile() {
			return( outf != null && new FileInfo( name+".0" ).Length > limit );
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
