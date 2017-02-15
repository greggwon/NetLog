using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLog.Logging
{
    public class ConsoleHandler : Handler
    {
        private bool consoleDebug;

        public new bool ConsoleDebug {
            get { return consoleDebug; }
            set { consoleDebug = value; }
        }
        public ConsoleHandler() {
            WaitCount = 100;
        }
        public override void Close() {
        }
        public override void Flush() {
            Console.Out.Flush();
        }

        //Queue recs = new Queue();
        public override void Publish( LogRecord rec ) {
            // stop now if not loggable
            if( consoleDebug )
                Console.WriteLine( "rec level: " + rec.Level + ", our Level: " + this.Level );

            if( rec.Level.IntValue < this.Level.IntValue || this.Level == Level.OFF ) {
                return;
            }
            Enqueue( rec );
        }

        /// <summary>
        /// The StringBuilder for buffering data with, to reduce Console.Write calls
        /// </summary>
        private StringBuilder bld = new StringBuilder();

        /// <summary>
        /// Push the passed LogRecord into the write buffer
        /// </summary>
        /// <param name="rec"></param>
        protected override void Push( LogRecord rec ) {
            // Make sure sequencing stays consistent and log records are
            // fully formed.
            lock(this) {
                rec.SequenceNumber = NextSequence;
                if( HavePrefix )
                    bld.Append( Prefix );
                bld.Append( this.Formatter.format( rec ) );
                if( HaveSuffix )
                    bld.Append( Suffix );
            }
        }

        /// <summary>
        /// Write the Buffer to the console.
        /// </summary>
        protected override void PushBoundry() {
            Console.Write( bld.ToString() );
            bld.Clear();
        }
    }
}
