namespace ActiverWebAPI.Models.DTO;

public class BranchDTO
{
    public int Id { get; set; }
    public string BranchName { get; set; }
    public string? Status { get; set; }
    public Dictionary<string, string>? DateStart { get; set; }
    public List<string>? DateEnd { get; set; }
    public List<string>? ApplyStart { get; set; }
    public List<string>? ApplyEnd { get; set; }
    public List<string>? ApplyFee { get; set; }
    public List<string>? Locations { get; set; }
}

public class BranchPostDTO
{
    public string BranchName { get; set; }
    public Dictionary<string, string> DateStart { get; set; }
    public List<string> DateEnd { get; set; }
    public List<string> ApplyStart { get; set; }
    public List<string> ApplyEnd { get; set; }
    public List<string> ApplyFee { get; set; }
    public List<string> Locations { get; set; }
}