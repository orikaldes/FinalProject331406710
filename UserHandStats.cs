using SQLite;

namespace FinalProject331406710
{
    [Table("UserHandStats")]
    public class UserHandStats
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("userId")]
        public string UserId { get; set; }

        [Column("handType")]
        public string HandType { get; set; }

        [Column("timesWon")]
        public int TimesWon { get; set; }

        [Column("totalMoneyWon")]
        public int TotalMoneyWon { get; set; }

        public UserHandStats() { }
    }
}