using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sfsp;
using Sfsp.Messaging;

namespace TestSFSP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<SfspHost> hosts;

        private void button2_Click(object sender, EventArgs e)
        {
            lst_hosts.Items.Clear();
            lst_hosts.Enabled = false;
            hosts = new List<SfspHost>();

            SfspScanner scanner = new SfspScanner(new SfspHostConfiguration("Boh"));

            scanner.StartScan(new TimeSpan(0,0,2));

            scanner.HostFound += (Object sender2, SfspHostFoundEventArgs e2) =>
            {
                addListItem(e2.Host.Name + " - " + e2.Host.Address.ToString());
                hosts.Add(e2.Host);
                //MessageBox.Show(e2.Host.Name + "\n" + e2.Host.Address.ToString());
            };

            scanner.ScanComplete += (Object sender2, EventArgs e2) =>
            {
                setListEnabled(true);
            };
        }

        delegate void setListEnabledDelegate(bool enabled);
        void setListEnabled(bool enabled)
        {
            if (lst_hosts.InvokeRequired)
                lst_hosts.Invoke(new setListEnabledDelegate(setListEnabled), new object[] { enabled });
            else
                lst_hosts.Enabled = enabled;
        }

        delegate void addListItemDelegate(string text);
        void addListItem(string text)
        {
            if (lst_hosts.InvokeRequired)
                lst_hosts.Invoke(new addListItemDelegate(addListItem), new object[] { text });
            else
                lst_hosts.Items.Add(text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txt_name.Enabled = false;

            SfspListener listener = new SfspListener(new SfspHostConfiguration(txt_name.Text));
            listener.TransferRequest += (object s2, TransferRequestEventArgs e2) =>
            {
                List<String> ss = e2.Download.GetObjects();
                MessageBox.Show(e2.Download.TotalSize.ToString() + "\n\n" + "Numero oggetti: " + ss.Count);

                e2.Download.ProgressUpdateTime = new TimeSpan(0, 0, 0, 0, 125);

                e2.Download.ProgressUpdate += (object s3, ProgressUpdateEventArgs e3) =>
                {
                    double perc = (double)e3.Progress / e3.TotalSize * 100;
                    UpdateText(label2, "Download: " + perc.ToString() + " %\n" + FormatBytes(e3.Speed) + "/s");
                };

                e2.Download.Accept("C:\\Users\\Luca\\Desktop\\test");
            };
            listener.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form2 frm2 = new Form2();
            frm2.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txt_name.Text = System.Net.Dns.GetHostName();
            // MessageBox.Show(String.Join("\n", Environment.GetCommandLineArgs()));

            this.Text = FormatBytes(123456789);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(SfspTests.PathUtils.GetRelativePath("C:\\Users\\Luca\\Documents\\Visual Studio 2015", "C:\\Users\\Luca\\Documents\\Visual Studio 2015\\Projects\\SFSP\\SfspLib\\SfspAsyncUpload.cs"));
            MessageBox.Show(SfspTests.PathUtils.GetRelativePath2("C:\\Users\\Luca\\Documents\\Visual Studio 2015", "C:\\Users\\Luca\\Documents\\Visual Studio 2015\\Projects\\SFSP\\SfspLib\\SfspAsyncUpload.cs"));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SfspHost selectedHost = hosts[lst_hosts.SelectedIndex];
            SfspAsyncUpload upload = selectedHost.Send(txt_path.Text);

            upload.StatusChanged += (object s2, TransferStatusChangedEventArgs e2) =>
            {
                if (e2.NewStatus == TransferStatus.Completed)
                    MessageBox.Show("Completato!");
                else if (e2.NewStatus == TransferStatus.Failed)
                    MessageBox.Show("Fallito!");
            };

            upload.ProgressUpdateTime = new TimeSpan(0, 0, 0, 0, 250);

            upload.ProgressUpdate += (object s2, ProgressUpdateEventArgs e2) =>
            {
                double perc = (double)e2.Progress / e2.TotalSize * 100;
                UpdateText(label1, "Upload: " + perc.ToString() + " %\n" + FormatBytes(e2.Speed) + "/s");
            };

            upload.Start();
        }

        private delegate void UpdateTextDelegate(Control c, string txt);
        private void UpdateText(Control c, string txt)
        {
            if (c.InvokeRequired)
            {
                c.Invoke(new UpdateTextDelegate(UpdateText), new object[] {c,  txt });
            }
            else
                c.Text = txt;
        }

        public string FormatBytes(long num)
        {
            if (num == 0)
                return "0 B";

            double n = num;
            string[] units = { "", "K", "M", "G", "T", "P", "E" };
            int ui = (int)Math.Truncate(Math.Log(n, 1024));

            n /= Math.Pow(1024, ui);

            string unit = units[ui];
            if (ui != 0)
                unit += "i";
            unit += "B";

            return String.Format("{0:0.#} {1}", n, unit);
        }
    }
}
