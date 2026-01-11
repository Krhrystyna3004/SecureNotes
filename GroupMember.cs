namespace SecureNotes
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string Permission { get; set; } = "edit"; // view | edit
    }
}