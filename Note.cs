using System;

namespace SecureNotes
{
    public class Note
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int? GroupId { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = ""; // текст або шифр
        public string Type { get; set; } = "note"; // note | password
        public string Color { get; set; } = "#FFFFFF";
        public string Tags { get; set; } = "";     // "work;todo"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string IvBase64 { get; set; } // IV для AES

        public bool IsEncrypted => Type == "password" && !string.IsNullOrEmpty(IvBase64);
    }
}