namespace NguyenVanThang_ASP.NET.Models
{
    public class Checkin
    {
        public int CheckinId { get; set; }
        public int TicketId { get; set; }
        public int StaffId { get; set; }
        public DateTime CheckinTime { get; set; }

        public Ticket Ticket { get; set; }
        public Staff Staff { get; set; }
    }
}
