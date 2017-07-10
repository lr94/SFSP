using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
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
            TransferObject = transfer;
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
                    _TransferObject.StatusChanged -= TransferObject_StatusChange;
                }

                _TransferObject = value;
                _TransferObject.ProgressUpdate += TransferObject_ProgressUpdate;
                _TransferObject.StatusChanged += TransferObject_StatusChange;
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

        public string Icon
        {
            get
            {
                if(_TransferObject is SfspAsyncDownload && _TransferObject.Status == TransferStatus.InProgress)
                {
                    return "Download";
                }
                if (_TransferObject is SfspAsyncUpload && _TransferObject.Status == TransferStatus.InProgress)
                {
                    return "Upload";
                }

                if(_TransferObject.Status == TransferStatus.Failed)
                {
                    return "ExclamationTriangle";
                }

                if(_TransferObject.Status == TransferStatus.Pending || _TransferObject.Status == TransferStatus.New)
                {
                    return "Spinner";
                }

                return "Check";
            }
        }

        public string SmallIcon
        {
            get
            {
                if (_TransferObject is SfspAsyncDownload)
                {
                    return "Download";
                }
                if (_TransferObject is SfspAsyncUpload)
                {
                    return "Upload";
                }

                return "Question"; // Non si dovrebbe mai arrivare qui
            }
        }

        public Visibility SmallIconVisibility
        {
            get
            {
                if (_TransferObject.Status == TransferStatus.Completed || _TransferObject.Status == TransferStatus.Failed)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Brush IconBrush
        {
            get
            {
                switch(_TransferObject.Status)
                {
                    case TransferStatus.Completed:
                        return Brushes.Green;
                        break;
                    case TransferStatus.Failed:
                        return Brushes.Red;
                        break;
                    case TransferStatus.InProgress:
                        return Brushes.Blue;
                        break;
                    default:
                        return Brushes.DarkGray;
                }
            }
        }

        public bool Spin
        {
            get
            {
                if (_TransferObject.Status == TransferStatus.Pending || _TransferObject.Status == TransferStatus.New)
                    return true;
                return false;
            }
        }

        public Visibility ProgressVisibility
        {
            get
            {
                if (_TransferObject.Status == TransferStatus.InProgress)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public bool CanBeStopped
        {
            get
            {
                return (_TransferObject.Status != TransferStatus.Failed && _TransferObject.Status != TransferStatus.Completed);
            }
        }
        
        private void TransferObject_ProgressUpdate(object sender, ProgressUpdateEventArgs e)
        {
            ProgressPercent = ((double)e.Progress) / TotalSize * 100;
            Speed = e.Speed;

            OnPropertyChanged("ProgressPercent");
            OnPropertyChanged("SpeedString");
        }

        private void TransferObject_StatusChange(object sender, TransferStatusChangedEventArgs e)
        {
            OnPropertyChanged("ProgressVisibility");
            OnPropertyChanged("IconBrush");
            OnPropertyChanged("Icon");
            OnPropertyChanged("Spin");
            OnPropertyChanged("SmallIconVisibility");
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
