namespace ConcertBookingApp.Models
{
    public class Utilisateur
    {
        public int UserID { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
        public string Adresse { get; set; }
        public List<Billet> Billets { get; set; }
    }
}
