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
		public static Level EVENTLOG = new Level("EVENTLOG", 899);
		public static Level WARNING = new Level("WARNING", 900);
		public static Level SEVERE = new Level( "SEVERE", 1000 );
		public static Level OFF = new Level( "OFF", int.MaxValue );
		public static Level ALL = new Level( "ALL", int.MinValue );

		///// <summary>
		/////  Adjust level up by the indicated number of 100 values
		///// </summary>
		///// <param name="l"></param>
		///// <param name="v"></param>
		///// <returns></returns>
		//public static Level operator +( Level l, int v ) {
		//	return LevelForIntValue(l.IntValue + ( v * 100 ), l.name+"+"+v);
		//}

		///// <summary>
		///// Adjust level down by the indicated number of 100 values
		///// </summary>
		///// <param name="l"></param>
		///// <param name="v"></param>
		///// <returns></returns>
		//public static Level operator -( Level l, int v ) {
		//	return LevelForIntValue(l.IntValue - ( v * 100 ), l.name+"-"+v);
		//}

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

		public static Level LevelForIntValue( int value ) {
			foreach( Level l in maps.Values ) {
				if( l.IntValue == value )
					return l;
			}
			return new Level(value);
		}

		/// <summary>
		/// Look for an existing level for the past value and map it to the passed name
		/// if it doesn't already exist.  If the name already exists, the current
		/// Level instance with that name is returned.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="newName"></param>
		/// <returns>The existing Level or a new Level created with the name and level</returns>
		public static Level LevelForIntValue( int value, string newName ) {
			foreach( string key in maps.Keys ) {
				Level l = maps[ key ];
				if( key.Equals(newName) )
					return l;
				if( l.IntValue == value )
					return l;
			}
			Level nl = new Level(newName, value);
			maps[ newName ] = nl;
			return nl;
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
