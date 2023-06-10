using ActiverWebAPI.Models.DBEntity;
using Newtonsoft.Json;

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
    public int UserVote { get; set; } = 0;
    public int TotalUserVote { get; set; } = 0;
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

public class ActivitySearchResponseDTO : SegmentsResponseBaseDTO<ActivityDTO>
{
    public string? Keyword { get; set; }
    public IEnumerable<TagBaseDTO>? Tags { get; set; }
    public string? Date { get; set; }
}

public class ActivitySearchRequestDTO : SegmentsRequestBaseDTO
{
    public string? Keyword { get; set; }
    public IEnumerable<string>? Tags { get; set; }
    public string? Date { get; set; }
}

public class ActivityCommentRequestDTO : SegmentsRequestBaseDTO
{
    public Guid ActivityId { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
}

public class ActivityCommentResponseDTO : SegmentsResponseBaseDTO<CommentDTO>
{
    public Guid ActivityId { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public CommentDTO? UserComment { get; set; }
}