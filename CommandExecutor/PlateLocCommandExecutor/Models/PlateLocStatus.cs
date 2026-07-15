using Serilog;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PlateLocCommandExecutor.Models
{
    public class PlateLocStatus : INotifyPropertyChanged
    {
        private static PlateLocStatus _instance = new PlateLocStatus();

        private PlateLocStatus() { }

        public static PlateLocStatus GetInstance()
        {
            return _instance;
        }

        /// <summary>
        /// The status of the instrument. These values are common to all instruments.
        /// </summary>
        public enum StatusCode
        {
            NotConnected = 0,
            Idle = 1,
            Running = 2,
            Error = 3,
        }

        private StatusCode _status;
        public StatusCode Status
        {
            get => _status;
            private set
            {
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }

        public bool IsStatusValid(StatusCode expextedStatus)
        {
            return IsStatusValid(new List<StatusCode> { expextedStatus });
        }

        public bool IsStatusValid(List<StatusCode> expectedStatuses)
        {
            return expectedStatuses.Any(s => s == Status);
        }

        public void UpdateStatus(StatusCode nextStatus)
        {
            if (nextStatus != Status)
            {
                Log.Information($"Update the status from {Status} to {nextStatus}.");
                Status = nextStatus;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
