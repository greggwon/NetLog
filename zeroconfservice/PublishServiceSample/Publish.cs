using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using ZeroconfService;

namespace PublishServiceSample
{
    public partial class Publish : Form
    {
        bool mPublishing = false;
        NetService publishService = null;

        public Publish()
        {
            InitializeComponent();
        }

        private void DoPublish()
        {
            try
            {
                Debug.WriteLine(String.Format("Bonjour Version: {0}", NetService.DaemonVersion));
            }
            catch (Exception ex)
            {
                String message = ex is DNSServiceException ? "Bonjour is not installed!" : ex.Message;
                MessageBox.Show(message, "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }

            String domain = "";
			String type = serviceTypeTextBox.Text;
			String name = serviceNameTextBox.Text;
			int port = Int32.Parse(portTextBox.Text);

			publishService = new NetService(domain, type, name, port);

			publishService.DidPublishService += new NetService.ServicePublished(publishService_DidPublishService);
			publishService.DidNotPublishService += new NetService.ServiceNotPublished(publishService_DidNotPublishService);

			/* HARDCODE TXT RECORD */
			System.Collections.Hashtable dict = new System.Collections.Hashtable();
			dict.Add("txtvers", "1");
			publishService.TXTRecordData = NetService.DataFromTXTRecordDictionary(dict);

			publishService.Publish();

			serviceNameTextBox.Enabled = false;
			serviceTypeTextBox.Enabled = false;
			portTextBox.Enabled = false;

			startStopButton.Enabled = false;
			startStopButton.Text = "Publishing...";

			mPublishing = true;
        }

		void publishService_DidPublishService(NetService service)
		{
			Console.WriteLine("Published Bonjour Service: domain({0}) type({1}) name({2})", service.Domain, service.Type, service.Name);

			startStopButton.Text = "Stop";
			startStopButton.Enabled = true;

			updateTXTButton.Enabled = true;
		}

		void publishService_DidNotPublishService(NetService service, DNSServiceException exception)
		{
			MessageBox.Show(String.Format("A DNSServiceException occured: {0}", exception.Message), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

			serviceNameTextBox.Enabled = true;
			serviceTypeTextBox.Enabled = true;
			portTextBox.Enabled = true;

			startStopButton.Text = "Publish";
			startStopButton.Enabled = true;

			mPublishing = false;
		}

        private void StopPublish()
        {
            if (publishService != null)
            {
                publishService.Stop();
            }

            serviceNameTextBox.Enabled = true;
            serviceTypeTextBox.Enabled = true;
            portTextBox.Enabled = true;

            startStopButton.Text = "Publish";

			updateTXTButton.Enabled = false;

            mPublishing = false;
        }

        private void startStopButton_Click(object sender, EventArgs e)
        {
            if (!mPublishing)
                DoPublish();
            else
                StopPublish();
        }

		private void updateTXTButton_Click(object sender, EventArgs e)
		{
			/* HARDCODE TXT RECORD */
			System.Collections.Hashtable dict = new System.Collections.Hashtable();
			dict.Add("txtvers", "2");
			dict.Add("CurrentTime", DateTime.Now.ToString());
            try 
	        {	        
				publishService.TXTRecordData = NetService.DataFromTXTRecordDictionary(dict);
                Debug.WriteLine("TXT Record updated");

        	}
	        catch (Exception)
	        {
				Debug.WriteLine("ERROR UPDATING TXT RECORD!");
	        }            
		}
    }
}