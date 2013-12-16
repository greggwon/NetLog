using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLog.Logging
{
	public class StreamFormatter : Formatter
	{
		private bool withTime = true;
		private bool brief = false;
		private bool trunc = false;
		private bool withClasses = true;
		private bool withMethods = true;
		internal static string fmt = "yyyy/MM/dd HH:mm:ss.fff", bfmt = "HH:mm:ss";

		public StreamFormatter()
		{
			Eol = "\n\r";
			Brief = false;
			WithTime = true;
			TruncatedPackageName = false;
		}

		/**
		 * The default is false, true, false.
		 */
		public StreamFormatter( bool brief, bool withTime, bool truncPkgName ) : this()
		{
			Brief = brief;
			WithTime = withTime;
			TruncatedPackageName = truncPkgName;
		}

		public bool Brief {
			get{ return brief; }
			set { brief = value; }
		}
		public bool WithTime {
			get { return withTime ; }
			set { withTime = value; }
		}
		public bool TruncatedPackageName {
			get { return trunc; }
			set { trunc = value; }
		}

		public bool WithClass {
			get { return withClasses; }
			set { withClasses = value; }
		}
		public bool WithMethod {
			get { return withMethods; }
			set { withMethods = value; }
		}

		public override string format( LogRecord rec ) {
			DateTime dt = rec.Millis;
			StringBuilder b = new StringBuilder();
			if( withTime )
				b.Append( dt.ToString(brief ? bfmt : fmt) );
			if( !brief ) {
				b.Append( withTime ? " [" : "[");
				if( trunc ) {
					String[] s = rec.LoggerName.Split("\\.".ToCharArray(0,2));

					// we could put a single character at each spot for the 'compacted' class name.
//					for( int i = 0; i < s.Length-1; ++i ) {
//						b.Append(s[i][0]);
//						b.Append(".");
//					}
					if( s.Length > 0 )
						b.Append(s[s.Length-1]);
					b.Append("#");
					b.Append(rec.SequenceNumber);
				}
				else
				{
					b.Append(rec.LoggerName);
					b.Append("#");
					b.Append(rec.SequenceNumber);
				}
				b.Append("] ");
			} else {
				if( withTime ) {
					b.Append( " " );
				}
			}
			b.Append( rec.Level );
			b.Append(" # ");

			if (withClasses && rec.SourceClassName != null )
			{
				b.Append(": from=");
				b.Append(rec.SourceClassName);
				if( withMethods && rec.SourceMethodName != null  )
					b.Append(".");
			}
			if (withMethods && rec.SourceMethodName != null )
			{
				if (!withClasses || rec.SourceClassName == null)
					b.Append(": from=");
				b.Append(rec.SourceMethodName);
				b.Append("(");
				Object[] a = rec.Parameters;
				if( a != null && a.Length > 0) {
					b.Append(" ");
					for( int i = 0; i < a.Length; ++i ) {
						if( i > 0 )
							b.Append(", ");
						b.Append( a[i]+"" );
					}
				}
				b.Append(" ) ");
			}
			b.Append( formatMessage( rec ) );
			if( rec.Thrown != null ) {
				b.Append( Eol );
				AddStackTrace( b, rec.Thrown );
			}
			b.Append( Eol );
			return b.ToString();
		}

		private void AddStackTrace( StringBuilder b, Exception exception ) {
			if( exception.StackTrace != null && exception.StackTrace.Length > 0 ) {
				if( exception.InnerException != null ) {
					AddStackTrace(b, exception.InnerException);
					b.Append(Eol);
				}
				b.Append("Wrapped in Exception: ");
				b.Append(exception.GetType().FullName);
				b.Append(Eol);
				b.Append(exception.StackTrace);
			} else {
				b.Append("*** No StackTrace Lines Present *** ");
			}
		}

		public string Eol { get; set; }
	}
}
