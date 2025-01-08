namespace Core.Models
{
    public class ReportRequest
    {
        public bool IncludeGeneralStatistics { get; set; }
        public bool IncludeActionHistory { get; set; }
        public bool IncludeInteractiveInfo { get; set; }
        public bool IncludeRatings { get; set; }
        public DateTime? StartDate { get; set; } // Початок періоду
        public DateTime? EndDate { get; set; } // Кінець періоду
        public int TopNoteCount { get; set; } = 3; // Кількість топ нот (за замовчуванням 3)
    }

}
