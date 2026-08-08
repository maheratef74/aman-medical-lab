using System;
using System.Collections.Generic;

namespace DrMohamedWeb.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalVisits { get; set; }
        public int TotalTestResults { get; set; }
        public int VisitsThisMonth { get; set; }
        public int VisitsLastMonth { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int NewPatientsLastMonth { get; set; }
        public int AvailableVisits { get; set; }
        public int HiddenVisits { get; set; }

        public List<string> MonthLabels { get; set; } = new();
        public List<int> PatientsPerMonth { get; set; } = new();
        public List<int> VisitsPerMonth { get; set; } = new();
        public List<int> ResultsPerMonth { get; set; } = new();

        public List<RecentVisitItem> RecentVisits { get; set; } = new();
        public List<RecentPatientItem> RecentPatients { get; set; } = new();
    }

    public class RecentVisitItem
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public int FilesCount { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class RecentPatientItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int VisitsCount { get; set; }
    }
}
