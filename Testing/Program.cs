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

namespace Testing {
	class Program {
		static Logger log = Logger.GetLogger(typeof(Program).FullName);
		static byte[]data = new byte[ 10240 ];
		static void Main( string[] args ) {
			new Program().TCPSocketTester();
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
