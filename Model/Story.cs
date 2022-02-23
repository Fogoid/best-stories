namespace BestStories.Model
{
    public class Story
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Uri { get; set; }
        public string? PostedBy { get; set; }
        public DateTime? Time { get; set; }
        public int Score { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
