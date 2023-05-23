namespace ActiverWebAPI.Models.DTO;

public class TagBaseDTO
{
    public int Id { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }
}

public class TagDTO : TagBaseDTO
{
    public int Trend { get; set; }
    public int TagVoteCount { get; set; }
    public bool UserVoted { get; set; }
    public int ActivityAmount { get; set; }
}

public class TagPostDTO
{
    public string Text { get; set; }
    public string Type { get; set; }
}

public class TagFilterDTO
{
    public string? Key { get; set; }
    public IEnumerable<string>? Type { get; set; }
}