namespace ActiverWebAPI.Models.DTO;

public class SegmentsRequestBaseDTO
{
    public string? OrderBy { get; set; }
    public int Page { get; set; } = 1;
    public int CountPerPage { get; set; } = 10;
}

public class SegmentsRequestDTO : SegmentsRequestBaseDTO
{
    public string? SortBy { get; set; }
}

public class SegmentsResponseBaseDTO<TEntity>
{
    public string? OrderBy { get; set; }
    public int CountPerPage { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPage { get; set; }
    public int TotalData { get; set; }
    public List<TEntity>? SearchData { get; set; }
}

public class SegmentsResponseDTO<TEntity> : SegmentsResponseBaseDTO<TEntity>
{
    public string? SortBy { get; set; }
}