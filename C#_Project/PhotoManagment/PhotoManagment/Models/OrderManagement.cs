namespace PhotoManagment.Models
{
    public class OrderManagement
    {
        private int _id;
        private string _OrderCode;
        private int _ProcessStatus;
        private string _OfficerCode;
        private string _CustomerCode;

        public int Id { 
            get {return _id ;} 
        }
        public string OrderCode 
        {
            get { return _OrderCode; }
            set 
            {
                if (_OrderCode != value && !string.IsNullOrEmpty(value))
                {
                    _OrderCode = value;
                }
            }
        }
        public int ProcessStatus {
            get { return _ProcessStatus; }
            set {; } 
        }
        public string OfficerCode {
            get { return _OfficerCode; }
            set {; }
        }
        public string CustomerCode {
            get { return _CustomerCode; }
            set {;}
        }

        public OrderManagement(string orderCode, int processStatus, string officerCode, string customerCode) 
        {
            OrderCode = orderCode;
            ProcessStatus = processStatus;
            OfficerCode = officerCode;
            CustomerCode = customerCode;
        }
    }
}
