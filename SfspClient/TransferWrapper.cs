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
            Progress = 0;
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

        public string ProgressBytesString
        {
            get
            {
                return NumericFormatter.FormatBytes(Progress);
            }
        }

        public string Description
        {
            get
            {
                if (_TransferObject.Status == TransferStatus.Completed)
                    return TotalSizeString;
                if (_TransferObject.Status == TransferStatus.Failed)
                {
                    Exception e = _TransferObject.FailureException;
                    if (e == null)
                        return TotalSizeString;

                    if (e is TransferAbortException)
                    {
                        var abortException = (TransferAbortException)e;
                        switch (abortException.Type)
                        {
                            case TransferAbortException.AbortType.LocalAbort:
                                return "Trasferimento annullato";
                            case TransferAbortException.AbortType.RemoteAbort:
                                return "Trasferimento annullato dall'host remoto";
                        }
                    }
                    else if (e is System.Net.Sockets.SocketException)
                        return "Errore di rete: " + e.Message;
                    else if (e is System.IO.IOException)
                        return "Errore di I/O: " + e.Message;
                }

                return ProgressBytesString + " / " + TotalSizeString;
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

        public string EstimatedTimeString
        {
            get
            {
                if (Speed == 0)
                    return "Eternità";

                long seconds = (TotalSize - Progress) / Speed;

                TimeSpan ts = new TimeSpan(TimeSpan.TicksPerSecond * seconds);
                return NumericFormatter.FormatTimeSpan(ts);
            }
        }

        public long Progress
        {
            get;
            set;
        }

        public double ProgressPercent
        {
            get
            {
                return ((double)Progress) / TotalSize * 100;
            }
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
                        return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    case TransferStatus.Failed:
                        return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                    case TransferStatus.InProgress:
                        return new SolidColorBrush(Color.FromRgb(26, 188, 156));
                    default:
                        return Brushes.DarkSlateGray;
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

        public Visibility DeleteMenuVisibility
        {
            get
            {
                if (_TransferObject.Status == TransferStatus.Completed || _TransferObject.Status == TransferStatus.Failed)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }
        
        private void TransferObject_ProgressUpdate(object sender, ProgressUpdateEventArgs e)
        {
            Progress = e.Progress;
            Speed = e.Speed;

            OnPropertyChanged("ProgressPercent");
            OnPropertyChanged("SpeedString");
            OnPropertyChanged("EstimatedTimeString");
        }

        private void TransferObject_StatusChange(object sender, TransferStatusChangedEventArgs e)
        {
            OnPropertyChanged("ProgressVisibility");
            OnPropertyChanged("IconBrush");
            OnPropertyChanged("Icon");
            OnPropertyChanged("Spin");
            OnPropertyChanged("SmallIconVisibility");
            OnPropertyChanged("DeleteMenuVisibility");
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
