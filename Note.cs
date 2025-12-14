using System;

namespace SecureNotes
{
    public class Note
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Type { get; set; } = "note"; // note | password | shared
        public string Color { get; set; } = "#FFFFFF";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string SharedWith { get; set; }
    }
}