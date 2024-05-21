using Aumerial.Data.Nti;
using ConcertBookingApp.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ConcertBookingApp.Data
{
    public class DataService : IDisposable
    {
        private readonly NTiConnection _dbConnection;

        public DataService(IConfiguration config)
        {
            _dbConnection = new NTiConnection();
            _dbConnection.DefaultDatabase = "CONCERT";
            _dbConnection.Username = Environment.GetEnvironmentVariable("user");
            _dbConnection.Password = Environment.GetEnvironmentVariable("password");
            _dbConnection.Server = Environment.GetEnvironmentVariable("server");
        }

        //Utilisateurs
        public IEnumerable<Utilisateur> GetAllUtilisateurs()
        {
            IEnumerable<Utilisateur> result = new List<Utilisateur>();
            result = _dbConnection.Query<Utilisateur>("SELECT * FROM Utilisateurs");
            foreach (var u in result)
            {
                u.Billets = GetUserBillets(u.UserID).ToList();
            }
            return result;
        }

        public Utilisateur? ValidateUtilisateur(string email, string password)
        {
            using (SHA1 instance = SHA1.Create())
            {
                password = Convert.ToBase64String(instance.ComputeHash(Encoding.Unicode.GetBytes(password)));
            }
            return _dbConnection.QueryFirstOrDefault<Utilisateur>("SELECT * FROM Utilisateurs WHERE EMAIL = ? AND MOTDEPASSE = ?", new { mail = email.ToLower(), password });
        }

        public Utilisateur GetUtilisateurById(int userId)
        {
            return _dbConnection.QueryFirstOrDefault<Utilisateur>("SELECT * FROM Utilisateurs WHERE UserID = ?", new { userId });
        }

        public int CreateUtilisateur(Utilisateur utilisateur, string password)
        {
            int count = _dbConnection.QueryFirst<int>("SELECT COUNT(*) AS CNT FROM UTILISATEURS WHERE EMAIL = ?", new { mail = utilisateur.Email.ToLower() });
            if (count > 0) return 0;

            using (SHA1 instance = SHA1.Create())
            {
                utilisateur.MotDePasse = Convert.ToBase64String(instance.ComputeHash(Encoding.Unicode.GetBytes(password)));
            }
            string sql = @"INSERT INTO Utilisateurs (Nom, Prenom, Email, MotDePasse, Adresse)
                           VALUES (?, ?, ?, ?, ?)";

            var parameters = new
            {
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email.ToLower(),
                MotDePasse = utilisateur.MotDePasse,
                Adresse = utilisateur.Adresse
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int UpdateUtilisateur(Utilisateur utilisateur)
        {
            string sql = @"UPDATE Utilisateurs
                           SET Nom = ?, Prenom = ?, Adresse = ?
                           WHERE UserID = ?";

            var parameters = new
            {
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Adresse = utilisateur.Adresse,
                UserId = utilisateur.UserID
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int DeleteUtilisateur(int userId)
        {
            string sql = @"DELETE FROM Utilisateurs WHERE UserID = ?";
            return _dbConnection.Execute(sql, new { userId });
        }
        //Concerts
        public IEnumerable<Concert> GetAllConcerts()
        {
            IEnumerable<Concert> result = _dbConnection.Query<Concert>("SELECT * FROM Concerts");
            foreach (var concert in result)
            {
                concert.Lieu = GetLieuById(concert.LieuID);
                concert.Artistes = _dbConnection.Query<Artiste>("SELECT * FROM Artistes WHERE ArtisteID IN (SELECT ARTISTEID FROM CONCERTARTISTE WHERE CONCERTID = ?)", new { concert.ConcertID }).ToList();
            }
            return result;

        }

        public Concert GetConcertById(int concertId)
        {
            Concert result = _dbConnection.QueryFirstOrDefault<Concert>("SELECT * FROM Concerts WHERE ConcertID = ?", new { concertId });
            result.Lieu = GetLieuById(result.LieuID);
            result.Artistes = _dbConnection.Query<Artiste>("SELECT * FROM Artistes WHERE ArtisteID IN (SELECT ARTISTEID FROM CONCERTARTISTE WHERE CONCERTID = ?)", new { result.ConcertID }).ToList();
            return result;
        }

        public void CreateConcert(Concert concert)
        {
            string sql = @"SELECT CONCERTID FROM FINAL TABLE (INSERT INTO Concerts (NomConcert, LieuID, DateConcert)
                           VALUES (?, ?, ?))";

            var parameters = new
            {
                NomConcert = concert.NomConcert,
                LieuID = concert.Lieu.LieuID,
                DateConcert = concert.DateConcert
            };
            concert.ConcertID = _dbConnection.QueryFirst<int>(sql, parameters);
            foreach (Artiste artiste in concert.Artistes)
            {
                _dbConnection.Execute("INSERT INTO CONCERTARTISTES (CONCERTID, ARTISTEID) VALUES (?, ?)", new { concert.ConcertID, artiste.ArtisteID });
            }
        }

        public int UpdateConcert(Concert concert)
        {
            string sql = @"UPDATE Concerts
                           SET NomConcert = ?, LieuID = ?, DateConcert = ?
                           WHERE ConcertID = ?";

            var parameters = new
            {
                NomConcert = concert.NomConcert,
                LieuID = concert.Lieu.LieuID,
                DateConcert = concert.DateConcert,
                ConcertID = concert.ConcertID
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int DeleteConcert(int concertId)
        {
            string sql = @"DELETE FROM Concerts WHERE ConcertID = ?";
            return _dbConnection.Execute(sql, new { concertId });
        }

        //Lieux
        public Lieu GetLieuById(int lieuId)
        {
            return _dbConnection.QueryFirstOrDefault<Lieu>("SELECT * FROM Lieux WHERE LieuID = ?", new { lieuId });
        }

        public int CreateLieu(Lieu lieu)
        {
            string sql = @"INSERT INTO Lieux (NomLieu, Adresse)
                           VALUES (?, ?)";

            var parameters = new
            {
                NomLieu = lieu.NomLieu,
                Adresse = lieu.Adresse
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int UpdateLieu(Lieu lieu)
        {
            string sql = @"UPDATE Lieux
                           SET NomLieu = ?, Adresse = ?
                           WHERE LieuID = ?";

            var parameters = new
            {
                NomLieu = lieu.NomLieu,
                Adresse = lieu.Adresse,
                LieuID = lieu.LieuID
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int DeleteLieu(int lieuId)
        {
            string sql = @"DELETE FROM Lieux WHERE LieuID = ?";
            return _dbConnection.Execute(sql, new { lieuId });
        }

        //Billets
        public IEnumerable<Billet> GetUserBillets(int userId)
        {
            IEnumerable<Billet> result = _dbConnection.Query<Billet>("SELECT * FROM Billets WHERE UserID = ?", new { userId });
            foreach (var b in result)
            {
                b.Concert = GetConcertById(b.ConcertID);
            }
            return result;
        }

        public int CreateBillet(Billet billet, Utilisateur user, Concert concert)
        {
            string sql = @"INSERT INTO Billets (UserID, ConcertID, NomSurBillet, PrenomSurBillet)
                           VALUES (?, ?, ?, ?)";

            var parameters = new
            {
                UserId = user.UserID,
                ConcertId = concert.ConcertID,
                NomSurBillet = billet.NomSurBillet,
                PrenomSurBillet = billet.PrenomSurBillet
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int UpdateBillet(Billet billet)
        {
            string sql = @"UPDATE Billets
                           SET NomSurBillet = ?, PrenomSurBillet = ?
                           WHERE BilletID = ?";

            var parameters = new
            {
                NomSurBillet = billet.NomSurBillet,
                PrenomSurBillet = billet.PrenomSurBillet,
                BilletID = billet.BilletID
            };

            return _dbConnection.Execute(sql, parameters);
        }

        public int DeleteBillet(int billetId)
        {
            string sql = @"DELETE FROM Billets WHERE BilletID = ?";
            return _dbConnection.Execute(sql, new { billetId });
        }

        public void ResetDb(ISnackbar snack)
        {
            _dbConnection.DefaultDatabase = string.Empty;
            _dbConnection.Open();
            try
            {
                _dbConnection.ExecuteClCommand("DLTLIB CONCERT");
            }
            catch
            {

            }
            try
            {
                _dbConnection.ExecuteClCommand("CRTLIB CONCERT");
            }
            catch
            {

            }
            _dbConnection.Close();
            _dbConnection.DefaultDatabase = "CONCERT";
            _dbConnection.Open();

            _dbConnection.Execute("CREATE TABLE Utilisateurs ( UserID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, Nom VARCHAR(50), Prenom VARCHAR(50), Email VARCHAR(100), MotDePasse VARCHAR(100), Adresse VARCHAR(200) )");
            _dbConnection.Execute("CREATE TABLE Lieux ( LieuID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, NomLieu VARCHAR(100), Adresse VARCHAR(200) )");
            _dbConnection.Execute("CREATE TABLE Concerts ( ConcertID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, NomConcert VARCHAR(100), LieuID INT, DateConcert DATE)");
            _dbConnection.Execute("CREATE TABLE Artistes ( ArtisteID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, NomArtiste VARCHAR(100), DescriptionArtiste VARCHAR(500) )");
            _dbConnection.Execute("CREATE TABLE Billets ( BilletID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY, UserID INT, ConcertID INT, NomSurBillet VARCHAR(50), PrenomSurBillet VARCHAR(50), Statut DECIMAL(1, 0) DEFAULT 0, DateScan TIMESTAMP DEFAULT CURRENT_TIMESTAMP)");
            _dbConnection.Execute("CREATE TABLE ConcertArtiste (ConcertID INT, ArtisteID INT)");

            _dbConnection.Execute("INSERT INTO LIEUX (NOMLIEU, ADRESSE) VALUES (? ,?)", new { nom = "Le Sucre", adresse = "50 Quai Rambaud, 69002 Lyon" });
            _dbConnection.Execute("INSERT INTO LIEUX (NOMLIEU, ADRESSE) VALUES (? ,?)", new { nom = "Zenith de Dijon", adresse = "Parc de la Toison d'Or, Rue de Colchide, 21000 Dijon" });
            _dbConnection.Execute("INSERT INTO LIEUX (NOMLIEU, ADRESSE) VALUES (? ,?)", new { nom = "La voile bleue", adresse = "Av. de Carnon, 34280 La Grande-Motte" });
            _dbConnection.Execute("INSERT INTO LIEUX (NOMLIEU, ADRESSE) VALUES (? ,?)", new { nom = "La Cigale", adresse = "120 Blvd Marguerite de Rochechouart, 75018 Paris" });
            _dbConnection.Execute("INSERT INTO LIEUX (NOMLIEU, ADRESSE) VALUES (? ,?)", new { nom = "Espace Evenements Georges Frêche", adresse = "8 Pl. du Foirail, 48000 Mende" });

            _dbConnection.Execute("INSERT INTO CONCERTS (NOMCONCERT, LIEUID, DATECONCERT) VALUES (? ,?, ?)", new { nom = "Nuits éléctroniques Lyon", lieuID = 1, Date = DateTime.Now.AddDays(25) });
            _dbConnection.Execute("INSERT INTO CONCERTS (NOMCONCERT, LIEUID, DATECONCERT) VALUES (? ,?, ?)", new { nom = "Dijon au rythme de la musette", lieuID = 2, Date = DateTime.Now.AddDays(150) });
            _dbConnection.Execute("INSERT INTO CONCERTS (NOMCONCERT, LIEUID, DATECONCERT) VALUES (? ,?, ?)", new { nom = "Laurent Garnier à la voile bleue", lieuID = 3, Date = DateTime.Now.AddDays(2) });
            _dbConnection.Execute("INSERT INTO CONCERTS (NOMCONCERT, LIEUID, DATECONCERT) VALUES (? ,?, ?)", new { nom = "Nuits éléctroniques Paris", lieuID = 4, Date = DateTime.Now.AddDays(6) });
            _dbConnection.Execute("INSERT INTO CONCERTS (NOMCONCERT, LIEUID, DATECONCERT) VALUES (? ,?, ?)", new { nom = "Mende au rythme de la musette", lieuID = 5, Date = DateTime.Now.AddDays(48) });

            _dbConnection.Execute("INSERT INTO ARTISTES (NOMARTISTE, DESCRIPTIONARTISTE) VALUES (? ,?)", new { nom = "Laurent Garnier", description = "Laurent Garnier, né le 1ᵉʳ février 1966 à Boulogne-Billancourt, est un DJ, compositeur et producteur français de musiques électroniques. Il a fondé et dirigé le label français F Communications. Le DJ et producteur a créé en 2013, aux côtés de Nicolas Galina et d'Arthur Durigon, le festival Yeah !" });
            _dbConnection.Execute("INSERT INTO ARTISTES (NOMARTISTE, DESCRIPTIONARTISTE) VALUES (? ,?)", new { nom = "Todd Terje", description = "odd Terje est un musicien, producteur, DJ et compositeur norvégien originaire de Mjøndalen et né en 1981. Son nom de scène rend hommage au DJ et compositeur de musique électronique Todd Terry." });
            _dbConnection.Execute("INSERT INTO ARTISTES (NOMARTISTE, DESCRIPTIONARTISTE) VALUES (? ,?)", new { nom = "Peggy Gou", description = "Peggy Gou, née Kim Min-ji le 3 juillet 1991 à Incheon, est une DJ, productrice musicale et styliste sud-coréenne." });
            _dbConnection.Execute("INSERT INTO ARTISTES (NOMARTISTE, DESCRIPTIONARTISTE) VALUES (? ,?)", new { nom = "Yvette Horner", description = "Yvette Marie Eugénie Hornère, dite Yvette Horner, née le 22 septembre 1922 à Tarbes et morte le 11 juin 2018 à Courbevoie, est une accordéoniste, pianiste et compositrice française." });
            _dbConnection.Execute("INSERT INTO ARTISTES (NOMARTISTE, DESCRIPTIONARTISTE) VALUES (? ,?)", new { nom = "Jerome Farges", description = "L'accordéoniste le plus célèbre de Lozère !" });



            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 1, artisteID = 1 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 1, artisteID = 2 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 1, artisteID = 3 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 2, artisteID = 4 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 2, artisteID = 5 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 3, artisteID = 1 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 4, artisteID = 1 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 4, artisteID = 2 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 4, artisteID = 3 });
            _dbConnection.Execute("INSERT INTO CONCERTARTISTE (CONCERTID, ARTISTEID) VALUES (? ,?)", new { concertID = 5, artisteID = 5 });
            snack.Add("Base de données réinitialisée !");
        }

        public void Dispose()
        {
            if (_dbConnection != null && _dbConnection.State == ConnectionState.Open)
            {
                _dbConnection.Close();
            }
        }
    }
}
