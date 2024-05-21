namespace ConcertBookingApp.Models
{
    public class Billet
    {
        public int BilletID { get; set; }
        public int ConcertID { get; set; }
        public Concert Concert { get; set; }
        public string NomSurBillet { get; set; }
        public string PrenomSurBillet { get; set; }
        public int Statut { get; set; }
        public DateTime DateScan { get; set; }
    }
}
