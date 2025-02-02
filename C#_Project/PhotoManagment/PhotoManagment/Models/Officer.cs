namespace PhotoManagment.Models
{
    public class Officer
    {
        private int _id;
        private string _OfficerCode;
        private string _Name;
        private string _Phone;

        public int Id 
        {
            get { return _id; } 
        }
        public string OfficerCode 
        {
            get { return _OfficerCode; } 
            set { ;} 
        }
        public string Name
        {
            get { return _Name; }
            set {; }
        }
        public string Phone
        {
            get { return _Phone; }
            set {; }
        }
    }
}
