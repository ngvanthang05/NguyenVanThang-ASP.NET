namespace NguyenVanThang_ASP.NET.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }

        public ICollection<Checkin> Checkins { get; set; }
    }
}
