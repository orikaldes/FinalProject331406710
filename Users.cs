using SQLite;

namespace FinalProject331406710
{
    [Table("Users")]
    public class Users
    {
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        [Column("fullName")]
        public string FullName { get; set; }

        [Column("age")]
        public int Age { get; set; }

        [Column("eMail")]
        public string Email { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("profileImagePath")]
        public string ProfileImagePath { get; set; }

        // --- NEW MAP COLUMNS (These were missing!) ---
        [Column("city")]
        public string City { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        public Users() { }

        public Users(string id, string fullName, int age, string email, string password)
        {
            this.Id = id;
            this.FullName = fullName;
            this.Age = age;
            this.Email = email;
            this.Password = password;
            this.ProfileImagePath = "";

            // Default location (0,0 means "Unknown")
            this.City = "";
            this.Latitude = 0;
            this.Longitude = 0;
        }
    }
}