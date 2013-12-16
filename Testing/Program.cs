using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLog.Logging;
using System.Configuration;
using System.Net.Sockets;
using System.Threading;
using System.Transactions;
using Microsoft.Practices.EnterpriseLibrary.Data;
using lufkin.iwellscada.v1_0.model.Transfers;
using lufkin.iwellscada.v1_0.model.DAOs;
using lufkin.iwellscada.v1_0.model;
using System.Data;
using System.Data.Common;
using System.IO;
using LWMPolling;
using ModbusPolling;
using DataItems;
using Newtonsoft.Json.Linq;

namespace Testing.Level1.Level2 {
	class Program : IPollingItemFromAccess, DataGatheringProvider {
		static Logger log = Logger.GetLogger(typeof(Program).FullName);
		static byte[]data = new byte[ 10240 ];
		static void Main( string[] args ) {
			new Program().LWMRecordTester();
		}

		public int[] ProcessItem( string well, PollingItem itm, TriggerRegisterRange r ) {
			return values.ToArray();
		}

		public object PollingItemValueFrom( string well, string name )
		{
			return values;
		}

		List<int>values = new List<int>();
		List<bool>statuses = new List<bool>();

		public void LWMRecordTester() {
			LWMApplication app = new LWMApplication( "Appname", 
				"lwm.xml", "records.xml", "schedules.xml", "polling.xml",
				new DestRTU( null, 1 ) );
			for( int i = 33; i < 4000; ++i ) {
				values.Add(((i<<8)|(i+1))&0xffff);
				statuses.Add(true);
			}
			StreamWriter strm = new StreamWriter(File.OpenWrite("c:/readtypes.json"));
			StringBuilder bld = new StringBuilder();
			bld.Append("{");
			//int cnt = 0;
			foreach( DeviceAccessType type in new DeviceAccessType[] { DeviceAccessType.ACCESS_COIL, DeviceAccessType.ACCESS_STATUS, DeviceAccessType.ACCESS_INPUT, DeviceAccessType.ACCESS_HOLDING } ) {
				foreach( PollingItem itm in 
					//new PollingItem[]{ app.deviceAccess.ReadTypeItem("LOAD_VFDSpeed_HISTORY_1440" )} ) {
						app.deviceAccess.ReadTypes(type).Values ) {
					if( itm.ReadtypeName == null )
						continue;
					string val;
					if( itm.CoilTriggersData ) {
						val = app.ConvertRecordValuesToJsonFor("test", itm, values, statuses, this);
					} else {
						val = app.ConvertItemValuesToJsonFor("test", app.deviceAccess.ReadTypeItem(itm.ReadtypeName),
							99, values, this, this);
					}

					bld.Append("\"" + itm.ReadtypeName + "\" : " + val + ",");
				}
			}
			bld.Append("\"empty\" : []}");
			string str = JObject.Parse(bld.ToString()).ToString();
			strm.WriteLine(str);
			strm.Close();
		}

		public void WebLogTester() {
			Setup();

			Logger.GetLogger("Testing").info("This is the logging");
		}

		public void Setup() {
			String path = ConfigurationManager.AppSettings[ "iWellScada.log" ];
			if( path == null ) {
				path = "c:\\programdata\\iWellLink\\Logs\\iWellScada.log";
			}
			DirectoryInfo di = new DirectoryInfo(new FileInfo(path).DirectoryName);
			if( di.Exists == false )
				di.Create();

			Logger top = Logger.GetLogger("");
			bool found = false;
			foreach( Handler h in top.GetHandlers() ) {
				if( h is FileHandler ) {
					found = true;
					break;
				}
			}
			if( !found ) {
				FileHandler fh = new FileHandler(path);
				fh.Generations = 20;
				fh.Limit = 20000000;
				top.AddHandler(fh);
			}
		}

		private void LambdaTester() {
			ActivityAnalysis a1 = new ActivityAnalysis("a1");
			ActivityAnalysis a2 = new ActivityAnalysis("a2");
			ActivityAnalysis a3 = new ActivityAnalysis("a3");
			ActivityAnalysis a4 = new ActivityAnalysis("a4");

			if( true ) {
				bool v1 = false, v2 = false;
				a1.TimeAction(() => {
					a2.TimeAction(() => {
						v1 = true;
					});
					a3.TimeAction(() => {
						v2 = true;
					});

				});
				log.info("v1: {0}, v2: {1}", v1, v2);
			}
		}

		private void LogTester() {
			Logger log1 = Logger.GetLogger( "Testing" );
			Logger log2 = Logger.GetLogger( "Testing.Level1" );
			Logger log3 = Logger.GetLogger( "Testing.Level1.Level2");
			log.log(Level.FINER, "Testing Level");
			log1.log(Level.FINER, "Testing Level1");
			log2.log(Level.FINEST, "Testing Level2");
			log3.log(Level.ALL, "Testing Level3");
		}

		private void TCPSocketTester() {
			
				TCPSocketHandler h = new TCPSocketHandler("localhost", 12310);
				while( true ) {
					Thread.Sleep(400);
					log.info("publishing next");
					h.Publish(new LogRecord(Level.INFO, "This is some text"));
				}

			
		}
		private void TestWellReadingDefinition() {
			DataSet ds = WellDAO.FindByWellId("24");
			DataSet wds = WellStateDAO.FindActiveStateByWellId("24");
			WellDTO w = new WellDTO(ds, wds);
			Utilities.Transaction<string>(( Database db, DbTransaction trans ) => {
				w.PopulateDeviceReadSources();
				w = DeviceReadTypeDAO.FindByDeviceId(w);
				w.RefreshWellReadingDefinitions("1", db, trans);
				DataSet set = DeviceReadTypeDAO.FindByReadTypeId("1984");
				DeviceReadTypeDTO drt = new DeviceReadTypeDTO("1", set.Tables[ 0 ].Rows[ 0 ][ "DeviceReadTypeId" ].ToString(), set.Tables[ 0 ].Rows[ 0 ][ "ReadTypeId" ].ToString() );
				drt.IsActive = false;
				DeviceReadTypeDAO.Update(drt, db, trans);

				return null;
			});
		}
	
		private void ActivityTester() {
			ActivityAnalysis a = new ActivityAnalysis("my analysis", new TimeSpan(0,0,5), 100);
			Random rnd = new Random((int)DateTime.Now.Ticks);
			while(true) {
				double val = rnd.NextDouble();
			//	log.info("next interval: {0}", val);
				a.TimeAction(() => {
					Thread.Sleep(new TimeSpan((long)(100000 * val)));
				});
			}
		}

		static void testLogging() {
			Logger log = Logger.GetLogger("");
			TCPSocketHandler hand;
			log.AddHandler( hand = new TCPSocketHandler());
			hand.PortNumber = 9998;
			log.info("hello");
			Random rnd = new Random((int)DateTime.Now.Ticks);
			new Thread(()=>{
				while( true ) {
					TcpClient cl = new TcpClient("localhost", 9998);
					Thread th = new Thread(() => {
						byte[]data = new byte[ 10240 ];
						int cnt;
						int total = 0;
						try {
							while( cl.Client.Connected && ( cnt = cl.GetStream().Read(data, 0, data.Length) ) > 0 ) {
								for( int i =0; i < cnt; ++i ) {
									Console.Write((char)data[ i ]);
								}
								total += cnt;
								if( total > rnd.NextDouble() * 10000 )
									break;
							}
							log.warning("Stopping logging");
							log.Flush();
						} finally {
							log.warning("closing listener socket");
							cl.Close();
						}
					});
					th.Start();
					log.info("joining listener thread");
					th.Join();
					log.warning("restarting listener thread");
				}
			}).Start();

			int lines = 1;
			while( true ) {
				log.info("more logging...{0}", lines++);
			}
		}

	}
}
