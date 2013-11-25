using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using ZeroconfService;

namespace BrowseServiceSample
{
    public partial class Browser : Form
    {
        NetServiceBrowser nsBrowser = new NetServiceBrowser();
        bool mBrowsing = false;

        public Browser()
        {
			InitializeComponent();

			Debug.WriteLine(String.Format("{0} Browser()", System.Threading.Thread.CurrentThread.ManagedThreadId));
			
			nsBrowser.InvokeableObject = this;
			nsBrowser.DidFindService += new NetServiceBrowser.ServiceFound(nsBrowser_DidFindService);
			nsBrowser.DidRemoveService += new NetServiceBrowser.ServiceRemoved(nsBrowser_DidRemoveService);
            nsBrowser.DidFindDomain += new NetServiceBrowser.DomainFound(nsBrowser_DidFindDomain);
            nsBrowser.SearchForRegistrationDomains();
            //nsBrowser.SearchForBrowseableDomains();
        }

        void nsBrowser_DidFindDomain(NetServiceBrowser browser, string domainName, bool moreComing)
        {
            Debug.WriteLine(browser, domainName);
        }

        void nsBrowser_DidRemoveService(NetServiceBrowser browser, NetService service, bool moreComing)
        {
			Debug.WriteLine(String.Format("{0}: nsBrowser_DidRemoveService: {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, service.Name));

            servicesList.BeginUpdate();
            foreach (ListViewItem item in servicesList.Items)
            {
                if (item.Tag == service)
                    servicesList.Items.Remove(item);
            }
            servicesList.EndUpdate();

			service.Dispose();
        }

        ArrayList waitingAdd = new ArrayList();
        void nsBrowser_DidFindService(NetServiceBrowser browser, NetService service, bool moreComing)
        {
			Debug.WriteLine(String.Format("{0}: nsBrowser_DidFindService: {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, service.Name));

			service.DidUpdateTXT += new NetService.ServiceTXTUpdated(ns_DidUpdateTXT);
			service.DidResolveService += new NetService.ServiceResolved(ns_DidResolveService);

			service.StartMonitoring();

            ListViewItem item = new ListViewItem(service.Name);
            item.Tag = service;

            if (moreComing)
            {
                waitingAdd.Add(item);
            }
            else
            {
                servicesList.BeginUpdate();
                while (waitingAdd.Count > 0)
                {
                    servicesList.Items.Add((ListViewItem)waitingAdd[0]);
                    waitingAdd.RemoveAt(0);
                }
                servicesList.Items.Add(item);
                servicesList.EndUpdate();
            }
        }

        private void startStopButton_Click(object sender, EventArgs e)
        {
			if (!mBrowsing)
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
			}

			if (!mBrowsing)
            {
                string service = serviceTextBox.Text;

                try
                {
                    nsBrowser.SearchForService(service, "");
                    mBrowsing = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Critical Error");
                }
            }
            else
            {
                nsBrowser.Stop();

                if (resolvingService != null)
                {
					resolvingService.Stop();
					resolvingService = null;
                }
				ClearTXTInfo();
                ClearResolveInfo();

                servicesList.BeginUpdate();
                servicesList.Items.Clear();
                servicesList.EndUpdate();

                mBrowsing = false;
            }

            if (mBrowsing)
            {
                startStopButton.Text = "Stop";
            }
            else
            {
                startStopButton.Text = "Start";
            }
        }

		private void ClearTXTInfo()
		{
			serviceLabel.Text = "No service selected";

			txtRecordLabel.Visible = false;
			txtRecordListView.Visible = false;

			txtRecordListView.BeginUpdate();
			txtRecordListView.Items.Clear();
			txtRecordListView.EndUpdate();
		}

        private void ClearResolveInfo()
        {
			hostnameLabel.Visible = false;
			resolveButton.Visible = false;

            addressLabel.Visible = false;
			addressList.Visible = false;

			addressList.BeginUpdate();
			addressList.Items.Clear();
			addressList.EndUpdate();
        }

        NetService resolvingService = null;
        private void Resolve(NetService service)
        {
			if (resolvingService != null)
            {
				resolvingService.Stop();
            }
			resolvingService = service;

            serviceLabel.Text = String.Format("Resolving '{0}'...", service.Name);
            
            service.ResolveWithTimeout(5);
        }

		void ns_DidResolveService(NetService service)
		{
            Debug.WriteLine(String.Format("Did Resolve Service: {0}", service.Name), "**************");

			// We only update the GUI if the service is currently selected
			ListView.SelectedListViewItemCollection slvic = servicesList.SelectedItems;

			NetService selected = null;
			if (slvic.Count > 0)
			{
				selected = (NetService)slvic[0].Tag;
			}

			if (service == selected)
			{
				resolveButton.Visible = true;

				hostnameLabel.Text = String.Format("Hostname: '{0}'", service.HostName);
				hostnameLabel.Visible = true;

				if (service.Addresses == null)
				{
					addressLabel.Visible = false;
					addressList.Visible = false;

					addressList.BeginUpdate();
					addressList.Items.Clear();
					addressList.EndUpdate();
				}
				else
				{
					addressLabel.Text = String.Format("{0} addresses:", service.Addresses.Count);
					addressList.BeginUpdate();
					addressList.Items.Clear();
					foreach (System.Net.IPEndPoint ep in service.Addresses)
					{
						addressList.Items.Add(new ListViewItem(ep.ToString()));
					}
					addressList.EndUpdate();
					addressLabel.Visible = true;
					addressList.Visible = true;
				}
			}
		}

		void ns_DidUpdateTXT(NetService service)
        {
            Debug.WriteLine(String.Format("Did Update TXT Record: {0}", service.Name), "**************");

			// We only update the GUI if the service is currently selected
			ListView.SelectedListViewItemCollection slvic = servicesList.SelectedItems;

			NetService selected = null;
			if (slvic.Count > 0)
			{
				selected = (NetService)slvic[0].Tag;
			}

			if (service == selected)
			{
				serviceLabel.Text = service.Name;

				byte[] txt = service.TXTRecordData;
				IDictionary dict = NetService.DictionaryFromTXTRecordData(txt);

				if (dict == null)
				{
					txtRecordLabel.Text = String.Format("No TXT Record Available");
					txtRecordListView.BeginUpdate();
					
					txtRecordListView.Items.Clear();
					
					txtRecordListView.EndUpdate();

					txtRecordLabel.Visible = true;
					txtRecordListView.Visible = true;
				}
				else
				{
					txtRecordLabel.Text = String.Format("{0} TXT Records:", dict.Count);
					txtRecordListView.BeginUpdate();

					txtRecordListView.Items.Clear();
					foreach (DictionaryEntry kvp in dict)
					{
						String key = (String)kvp.Key;
						byte[] value = (byte[])kvp.Value;

						// If you were creating your own service, or browsing a known service,
						// then you'd know what kind of data to expect as the value.
						// But we don't here, so we assume UTF8 strings.

						string valueStr;
						try
						{
							valueStr = Encoding.UTF8.GetString(value);
						}
						catch
						{
							valueStr = "[Binary Data]";
						}

						ListViewItem newitem = new ListViewItem(key);
						newitem.SubItems.Add(valueStr);
						
						txtRecordListView.Items.Add(newitem);
					}

					txtRecordListView.EndUpdate();

					txtRecordLabel.Visible = true;
					txtRecordListView.Visible = true;
				}
			}
		}

		private void servicesList_SelectedIndexChanged(object sender, EventArgs e)
		{
			ListView.SelectedListViewItemCollection slvic = servicesList.SelectedItems;

			NetService selected = null;
			if (slvic.Count > 0)
			{
			    selected = (NetService)slvic[0].Tag;
			}

			if (selected != null)
			{
				ns_DidUpdateTXT(selected);
				ns_DidResolveService(selected);
			}
			else
			{
				ClearTXTInfo();
			    ClearResolveInfo();
			}
		}

		private void resolveButton_Click(object sender, EventArgs e)
		{
			ListView.SelectedListViewItemCollection slvic = servicesList.SelectedItems;
			if (slvic.Count > 0)
			{
				NetService selected = (NetService)slvic[0].Tag;
				Resolve(selected);
			}
		}
	}
}