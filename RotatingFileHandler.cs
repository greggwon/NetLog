﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLog.Logging;
using System.IO;

namespace NetLog.Logging {
	public class RotatingFileHandler : FileHandler {
		private string filenameFormat = "yyyyMMdd";
		private string openFormat;

		public RotatingFileHandler() {
		}
		public RotatingFileHandler ( String file ) : this( file, "yyyyMMdd" ) {
		}
		public RotatingFileHandler( String file, String pattern ) : base( file ) {
			filenameFormat = pattern;
			openFormat = DateTime.Now.ToString( pattern );
		}
		public String CheckPattern {
			get {return filenameFormat;}
			set {
				lock ( this ) {
					filenameFormat = value;
					if( CheckNewFile() ) {
						Flush();
						Close();
					}
				}
			}
		}
		public override string baseFileName ( string path ) {
			return DateTime.Now.ToString( path );
		}

		protected override bool CheckNewFile() {
			if( openFormat == null )
				return true;
			return openFormat.Equals( DateTime.Now.ToString( CheckPattern ) ) == false;
		}

		protected override void shuffleDown() {
			// do nothing to shuffle files
		}

		protected override StreamWriter baseFileOpen( bool append ) {
			string rname = Filename;
			StreamWriter outp = null;
			int cnt = 1;
			string nm = null;
			do {

				try {
					outp = new StreamWriter( nm = DateTime.Now.ToString( rname), append );
					outp.AutoFlush = false;
					finfo = new FileInfo( nm );
				} catch ( IOException ex ) {

					Console.WriteLine( "# SEVERE # Error opening log file (name=\"" + rname + "\") for writing: " + ex.Message + "\n" + ex.StackTrace );
					rname = Filename + "-" + cnt++;
					Console.WriteLine( "Trying file: \"" + rname + "\" next" );
				}
			} while ( outp == null );
			Filename = rname;
			openFormat = DateTime.Now.ToString( CheckPattern ) ;
			return outp;
		}
	}
}
