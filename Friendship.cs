using SQLite;
using System;

namespace FinalProject331406710
{
    [Table("Friendships")]
    public class Friendship
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("senderId")]
        public string SenderId { get; set; }

        [Column("receiverId")]
        public string ReceiverId { get; set; }

        // 0 = Pending, 1 = Accepted
        [Column("status")]
        public int Status { get; set; }

        public Friendship() { }

        public Friendship(string sender, string receiver, int status)
        {
            SenderId = sender;
            ReceiverId = receiver;
            Status = status;
        }
    }
}