﻿namespace ActiverWebAPI.Models.DTO;

public class BranchDTO
{
    public int Id { get; set; }
    public string BranchName { get; set; }
    public List<string>? Location { get; set; }
    public List<BranchDateDTO>? Date { get; set; }
}

public class BranchPostDTO
{
    public string BranchName { get; set; }
    public List<string>? Location { get; set; }
    public List<BranchDateDTO>? Date { get; set; }
}

public class BranchDateDTO
{
    public string Name { get; set; }
    public string? Start { get; set; }
    public string? End { get; set; }
}