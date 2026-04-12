// Models/BusRoute.cs — thêm BasePrice
namespace NguyenVanThang_ASP.NET.Models
{
    public class BusRoute
    {
        public int BusRouteId { get; set; }
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double Distance { get; set; }       // km
        public int Duration { get; set; }           // phút
        public decimal BasePrice { get; set; }      // giá vé cơ bản

        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}