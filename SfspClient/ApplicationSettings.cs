﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
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

        public ApplicationSettings()
        {
            HostName = System.Net.Dns.GetHostName();
            DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            AutoAccept = false;
            Mode = HostMode.Online;
            Loopback = true;
            MulticastAddress = IPAddress.Parse("239.0.0.2");
            UdpPort = 5999;
            TcpPort = 6000;

            Load();
        }

        /// <summary>
        /// Ottiene il nome della cartella relativa all'applicazione all'interno di AppData
        /// </summary>
        /// <returns></returns>
        private string GetApplicationFolder()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appdata, ProductName);
        }

        private string GetFileName()
        {
            return Path.Combine(GetApplicationFolder(), SettingsFile);
        }

        private void CreateFile()
        {
            string appdata = GetApplicationFolder();
            if (!Directory.Exists(appdata))
                Directory.CreateDirectory(appdata);

            if (!File.Exists(GetFileName()))
            {
                data = new Dictionary<string, string>();
                Store();
            }
        }

        /// <summary>
        /// Carica nuovamente le impostazioni
        /// </summary>
        public void Load()
        {
            string fname = GetFileName();

            // Se il file delle impostazioni non esiste lo creo
            if (!File.Exists(fname))
                CreateFile();

            // Carico le impostazioni nella mappa
            data = new Dictionary<string, string>();

            StreamReader sr = File.OpenText(fname); // UTF-8
            
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
            }

            sr.Close();

            // Imposto le proprietà

            bool set_default = false; // Indica se dobbiamo impostare delle proprietà di default

            // Se name o path sono vuote salviamo i dati di default determinati dinamicamente
            string val = ReadString("name");
            if (String.IsNullOrEmpty(val))
                set_default = true;
            else
                HostName = val;

            val = ReadString("path");
            if (String.IsNullOrEmpty(val))
                set_default = true;
            else
                DefaultPath = val;

            AutoAccept = ReadBoolean("autoaccept");

            Mode = ReadHostMode("mode");

            MulticastAddress = ReadIPAddress("multicast_address");

            Loopback = ReadBoolean("loopback");

            UdpPort = ReadInteger("udp_port");
            if (UdpPort > short.MaxValue)
                throw new FileFormatException("Invalid port number" + UdpPort);
            TcpPort = ReadInteger("tcp_port");
            if (TcpPort > short.MaxValue)
                throw new FileFormatException("Invalid port number" + TcpPort);

            if (set_default)
                Store();
        }

        /// <summary>
        /// Salva le impostazioni correntemente rappresentate dall'oggetto
        /// </summary>
        public void Store()
        {
            WriteString("name", HostName);
            WriteString("path", DefaultPath);
            WriteHostMode("mode", Mode);
            WriteBoolean("autoaccept", AutoAccept);
            WriteIPAddress("multicast_address", MulticastAddress);
            WriteBoolean("loopback", Loopback);
            WriteInteger("udp_port", UdpPort);
            WriteInteger("tcp_port", TcpPort);

            StreamWriter sw = new StreamWriter(GetFileName(), false, Encoding.UTF8);

            sw.WriteLine("# Do not manually edit this file");
            foreach (string k in data.Keys)
                sw.WriteLine(k + " = " + data[k]);

            sw.Close();
        }

        private void WriteString(string key, string value)
        {
            data[key] = "\"" + value + "\"";
        }

        private void WriteInteger(string key, int value)
        {
            data[key] = value.ToString();
        }

        private void WriteBoolean(string key, bool value)
        {
            if (value)
                data[key] = "true";
            else
                data[key] = "false";
        }

        private void WriteIPAddress(string key, IPAddress value)
        {
            data[key] = value.ToString();
        }

        private void WriteHostMode(string key, HostMode value)
        {
            if (value == HostMode.Online)
                data[key] = "online";
            else if (value == HostMode.Offline)
                data[key] = "offline";
        }

        private string ReadString(string key)
        {
            if (!data.ContainsKey(key))
                return "";

            string val = data[key];

            if (!(val.StartsWith("\"") && val.EndsWith("\"")))
                throw new FileFormatException("Invalid string in settings file");

            return val.Substring(1, val.Length - 2);
        }

        private bool ReadBoolean(string key)
        {
            if (!data.ContainsKey(key))
                return false;

            string val = data[key].ToLower();

            if (val == "true")
                return true;
            else if (val == "false")
                return false;
            else
                throw new FileFormatException("Invalid boolean in settings file");
        }

        private IPAddress ReadIPAddress(string key)
        {
            if (!data.ContainsKey(key))
                return null;

            IPAddress parsed;

            if (!IPAddress.TryParse(data[key], out parsed))
                throw new FileFormatException("Invalid IP address in settings file");

            return parsed;
        }

        private HostMode ReadHostMode(string key)
        {
            if (!data.ContainsKey(key))
                return HostMode.Online;

            string val = data[key].ToLower();

            if (val == "online")
                return HostMode.Online;
            else if (val == "offline")
                return HostMode.Offline;
            else
                throw new FileFormatException("Invalid host mode in settings file");
        }

        private int ReadInteger(string key)
        {
            if (!data.ContainsKey(key))
                return 0;

            return Int32.Parse(data[key]);
        }

        /// <summary>
        /// Nome dell'host
        /// </summary>
        public string HostName
        {
            get;
            set;
        }

        /// <summary>
        /// Percorso di destinazione di default
        /// </summary>
        public string DefaultPath
        {
            get;
            set;
        }

        /// <summary>
        /// Specifica se l'host deve accettare automaticamente i file in arrivo
        /// </summary>
        public bool AutoAccept
        {
            get;
            set;
        }

        /// <summary>
        /// Specifica se l'host deve operare in modalità Online o Offline
        /// </summary>
        public HostMode Mode
        {
            get;
            set;
        }

        /// <summary>
        /// Specifica l'indirizzo del gruppo multicast su cui deve lavorare l'applicazione
        /// </summary>
        public IPAddress MulticastAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Specifica se l'host deve potersi autoinviare dei file
        /// </summary>
        public bool Loopback
        {
            get;
            set;
        }

        /// <summary>
        /// Specifica la porta UDP su cui restare in ascolto
        /// </summary>
        public int UdpPort
        {
            get;
            set;
        }

        /// <summary>
        /// Specifica la porta TCP su cui restare in ascolto
        /// </summary>
        public int TcpPort
        {
            get;
            set;
        }
    }
}
