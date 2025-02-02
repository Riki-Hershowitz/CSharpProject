namespace PhotoManagment.Models
{
    public class Status
    {
        private int _id { get; set; }
        private int _ProcessStatus { get; set; }
        private string _StatusDescription { get; set; }

        public int Id
        {
            get { return _id; }
        }
        public int ProcessStatus
        {
            get { return _ProcessStatus; }
            set {; }
        }
        public string StatusDescription
        {
            get { return _StatusDescription; }
            set {; }
        }

        public Status (int processStatus, string statusDescription)
        {
            ProcessStatus = processStatus;
            StatusDescription = statusDescription;
        }
    }
}
