namespace Server.ViewModels.History
{
    public class ReportRequestModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeGeneralStatistics { get; set; }
        public bool IncludeActionHistory { get; set; }
        public bool IncludeInteractiveInfo { get; set; }
        public int TopNoteCount { get; set; }
    }
}
