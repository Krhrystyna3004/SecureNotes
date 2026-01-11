using System;

namespace SecureNotes
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string PasswordSalt { get; set; } = "";
        public string PinHash { get; set; } = "";
        public string PinSalt { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string PreferredTheme { get; set; } = "Light"; // Light|Dark
    }
}