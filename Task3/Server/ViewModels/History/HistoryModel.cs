namespace Server.ViewModels.History
{
    public class HistoryModel
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string ExecutionStatus { get; set; }
        public string DeviceName { get; set; }
        public string ElixirName { get; set; }
    }
}
