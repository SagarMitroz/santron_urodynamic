using System;

namespace SantronWinApp
{
    internal class ReportData
    {
        public string FontFamily { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public string PatientAddress { get; set; }
        public string PatientMobile { get; set; }
        public string DoctorPost { get; set; }
        public int Age { get; set; }
        public string DoctorName { get; set; }
        public string HospitalAddressLine1 { get; set; }
        public string HospitalName { get; set; }
        public string DoctorDegree { get; set; }
        public DateTime TestDate { get; set; }
    }
}