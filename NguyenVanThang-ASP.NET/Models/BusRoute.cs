namespace NguyenVanThang_ASP.NET.Models
{
    public class BusRoute
    {
        public int BusRouteId { get; set; }

        public string Departure { get; set; }
        public string Destination { get; set; }
        public double Distance { get; set; }
        public int Duration { get; set; }

        public ICollection<Trip> Trips { get; set; }
    }
}
