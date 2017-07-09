using System;
using System.ComponentModel;
using Sfsp;

namespace SfspClient
{
    class TransferWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public TransferWrapper(SfspAsyncTransfer transfer, string remoteHostName, string rootObjectName)
        {
            ProgressPercent = 0.0;
            Speed = 0;
            RemoteHostName = remoteHostName;
            _TransferObject = transfer;
            RootObjectName = rootObjectName;
        }

        private SfspAsyncTransfer _TransferObject;
        public SfspAsyncTransfer TransferObject
        {
            get
            {
                return _TransferObject;
            }
            set
            {
                if(_TransferObject != null)
                {
                    _TransferObject.ProgressUpdate -= TransferObject_ProgressUpdate;
                }

                _TransferObject = value;
                _TransferObject.ProgressUpdate += TransferObject_ProgressUpdate;
            }
        }

        public long TotalSize
        {
            get
            {
                return _TransferObject.TotalSize;
            }
        }

        public long Speed
        {
            get;
            private set;
        }

        public string TotalSizeString
        {
            get
            {
                return NumericFormatter.FormatBytes(TotalSize);
            }
        }

        public string SpeedString
        {
            get
            {
                return NumericFormatter.FormatBytes(Speed) + "/s";
            }
        }

        public double ProgressPercent
        {
            get;
            set;
        }

        public string RemoteHostName
        {
            get;
            private set;
        }
        
        public string RootObjectName
        {
            get;
            private set;
        }
        
        private void TransferObject_ProgressUpdate(object sender, ProgressUpdateEventArgs e)
        {
            ProgressPercent = ((double)e.Progress) / TotalSize * 100;
            Speed = e.Speed;

            OnPropertyChanged("ProgressPercent");
            OnPropertyChanged("SpeedString");
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
