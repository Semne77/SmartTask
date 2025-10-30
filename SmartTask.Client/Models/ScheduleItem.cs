namespace SmartTask.Client.Models
{
    public class ScheduleItem
    {
        public string Date { get; set; } = "";
        public string TimeStart { get; set; } = "";
        public string TimeFinish { get; set; } = "";
        public string ClassCategory { get; set; } = "";
        public string ClassName { get; set; } = "";
        public double Duration { get; set; }
    }
}
