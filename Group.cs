using System;

namespace SecureNotes
{
    public class Group
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string InviteCode { get; set; } = "";
        public string Name { get; set; } = "Моя група";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public override string ToString() => Name;
    }
}