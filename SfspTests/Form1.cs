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
    enum test : byte
    {
        a = 1,
        b = 2
    }

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
                string txt = "";
                foreach(String s in ss)
                {
                    txt += s + "\n";
                }
                MessageBox.Show(e2.Download.TotalSize.ToString() + "\n\n" + txt);
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
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(SfspTests.PathUtils.GetRelativePath("C:\\Users\\Luca\\Documents\\Visual Studio 2015", "C:\\Users\\Luca\\Documents\\Visual Studio 2015\\Projects\\SFSP\\SfspLib\\SfspAsyncUpload.cs"));
            MessageBox.Show(SfspTests.PathUtils.GetRelativePath2("C:\\Users\\Luca\\Documents\\Visual Studio 2015", "C:\\Users\\Luca\\Documents\\Visual Studio 2015\\Projects\\SFSP\\SfspLib\\SfspAsyncUpload.cs"));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SfspHost selectedHost = hosts[lst_hosts.SelectedIndex];
            SfspAsyncUpload upload = selectedHost.Send("C:\\Users\\Luca\\Documents\\visual studio 2015\\Projects\\SFSP\\SfspTests\\bin\\Debug\\Sfsp.dll");
            upload.Start();
        }
    }
}
