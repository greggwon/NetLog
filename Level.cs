using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;

namespace NetLog.Logging
{
	public class Level
	{
		private static Dictionary<String, Level> maps = new Dictionary<String, Level>();
		public static Level FINEST = new Level("FINEST", 300);
		public static Level FINER = new Level( "FINER", 400);
		public static Level FINE = new Level( "FINE", 500 );
		public static Level CONFIG = new Level( "CONFIG", 700);
		public static Level INFO = new Level(  "INFO", 800);
		public static Level WARNING = new Level( "WARNING", 900 );
		public static Level EVENTLOG = new Level( "EVENTLOG", 999 );
		public static Level SEVERE = new Level( "SEVERE", 1000 );
		public static Level OFF = new Level( "OFF", int.MaxValue );
		public static Level ALL = new Level( "ALL", int.MinValue );

		private int value;
		private string name;
		private static bool consoleDebug;

		public static bool ConsoleDebug
		{
			get { return consoleDebug; }
			set { consoleDebug = value; }
		}

		private Level(int val)
		{
			value = val;
			maps["Level-"+val] = this;
		}
		public Level( string name, int val )
		{
			value = val;
			this.name = name;
			if( maps == null ) {
				maps = new Dictionary<String, Level>();
			}
			if( maps.ContainsKey(name) == false ) {
				maps[name] = this;
			}
		}

		public int IntValue {
			get { return value; }
		}
		public static Level parse( string name ) {
			if(consoleDebug)
				Console.WriteLine( "looking for level named \""+name+"\": "+maps.ContainsKey(name));
			if( maps.ContainsKey(name) )
				return (Level)maps[name];
			return Level.ALL;
		}

		public override string ToString() {
			return name;
		}

		public string GetLocalizedName() {
			return name;
		}

		public string GetName() {
			return name;
		}
	}
}
