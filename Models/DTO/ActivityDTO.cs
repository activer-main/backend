using ActiverWebAPI.Models.DBEntity;

namespace ActiverWebAPI.Models.DTO;

public class ActivityDTO
{
    public Guid Id { get; set; }
    public int Trend { get; set; }
    public string Title { get; set; }
    public string? SubTitle { get; set; }
    public string Content { get; set; }
    public string? Html { get; set; }
    public string? Status { get; set; }
    public DateTime? AddTime { get; set; }
    public DateTime CreateAt { get; set; }
    public List<string>? Fee { get; set; }
    public List<string>? Images { get; set; }
    public List<string>? Connections { get; set; }
    public List<string>? Holders { get; set; }
    public List<string>? Objectives { get; set; }
    public List<string>? Sources { get; set; }
    public List<BranchDTO> Branches { get; set; }
    public List<TagDTO>? Tags { get; set; }
}

public class ActivityPostDTO
{
    public string Title { get; set; }
    public string? SubTitle { get; set; }
    public string Content { get; set; }
    public string? Html { get; set; }
    public List<string>? Images { get; set; }
    public List<string>? Connections { get; set; }
    public List<string>? Holders { get; set; }
    public List<string>? Objectives { get; set; }
    public List<string>? Sources { get; set; }
    public List<BranchPostDTO> Branches { get; set; }
    public List<TagPostDTO>? Tags { get; set; }
}

public class ActivitySegmentDTO : SegmentsRequestDTO
{
    public new string? SortBy { get; set; } = "CreatedAt";
    public List<string>? Tags { get; set; } = new() { };
    public List<string>? Status { get; set; } = new() { };
}

public class ActivitySegmentResponseDTO : SegmentsResponseDTO<ActivityDTO>
{
    public List<string>? Tags { get; set; }
    public List<string>? Status { get; set; }
}

public class ActivityFilterDTO
{
    public IEnumerable<TagDTO>? Tags { get; set; }
    public IEnumerable<string>? Status { get; set; }
}