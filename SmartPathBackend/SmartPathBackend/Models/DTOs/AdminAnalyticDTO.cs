namespace SmartPathBackend.Models.DTOs
{
    public class DailyCountDto { public DateTime Date { get; set; } public int Count { get; set; } }

    public class ActivityDailyDto
    {
        public DateTime Date { get; set; }
        public int Posts { get; set; }
        public int Comments { get; set; }
        public int Reactions { get; set; }
        public int Reports { get; set; }
        public int NewUsers { get; set; }
    }


    public class UserAdminSummaryDto
    {
        public UserResponseDto User { get; set; } = default!;
        public int Posts { get; set; }
        public int Comments { get; set; }
        public int Reactions { get; set; }
        public int Friends { get; set; }
        public int ReportsAgainst { get; set; } 
        public int ReportsFiled { get; set; } 
    }
}
