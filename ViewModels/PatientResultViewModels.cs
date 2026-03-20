using System;
using System.Collections.Generic;

namespace DrMohamedWeb.ViewModels
{
    public class PatientResultViewModel
    {
        public string PatientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public VisitViewModel? LatestVisit { get; set; }
        public List<VisitViewModel> AllVisits { get; set; } = new List<VisitViewModel>();
    }

    public class VisitViewModel
    {
        public int Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string? Notes { get; set; }
        public List<TestResultViewModel> TestResults { get; set; } = new List<TestResultViewModel>();
    }

    public class TestResultViewModel
    {
        public int Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
