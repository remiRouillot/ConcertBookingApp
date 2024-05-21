namespace ConcertBookingApp.Models
{
    public class Concert
    {
        public int ConcertID { get; set; }
        public string NomConcert { get; set; }
        public int LieuID { get; set; }
        public Lieu Lieu { get; set; }
        public DateTime DateConcert { get; set; }
        public List<Artiste> Artistes { get; set; }
    }
}
