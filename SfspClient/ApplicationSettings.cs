using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SfspClient
{
    internal class ApplicationSettings
    {
        public enum HostMode
        {
            Online, Offline
        }

        private const string ProductName = "Sfsp";
        private const string SettingsFile = "settings.conf";

        private Dictionary<string, string> data;

        public string GetApplicationFolder()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appdata, ProductName);
        }

        public string GetFileName()
        {
            return Path.Combine(GetApplicationFolder(), SettingsFile);
        }

        public void Load()
        {
            data = new Dictionary<string, string>();

            StreamReader sr = File.OpenText(GetFileName());

            while(!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (line.StartsWith("#"))
                    continue;

                int equal_index = line.IndexOf('=');
                if (equal_index < 0)
                    throw new FileFormatException("Invalid settings file");

                string key = line.Substring(0, equal_index).Trim();
                string value = line.Substring(equal_index + 1).Trim();

                data.Add(key, value);

                System.Windows.MessageBox.Show(key + "\n" + value);
            }
        }

        public string HostName
        {
            get;
            set;
        }

        public string DefaultPath
        {
            get;
            set;
        }

        public bool AutoAccept
        {
            get;
            set;
        }

        public HostMode Mode
        {
            get;
            set;
        }
    }
}
