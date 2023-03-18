using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ActiverWebAPI.Models.DBEntity;

public class Activity : BaseEntity
{
    public Guid ActivityId { get; set; }

    [Required]
    public string Title { get; set; }
    public string? Subtitle { get; set; }

    [Column(TypeName="text")]
    public string Content { get; set; }
    public int ActivityClickedCount { get; set; } = 0;

    public List<Branch> Branches { get; set; }
    public List<Image>? Images { get; set; }
    public List<Source>? Sources { get; set; }
    public List<Connection>? Connections { get; set; }
    public List<Holder>? Holders { get; set; }
    public List<Objective>? Objectives { get; set; }
    public List<UserVoteTagInActivity>? UserVoteTagInActivity { get; set; }
}

public class Image : BaseEntity
{
    public int ImageId { get; set; }
    public string ImageURL { get; set; }

    [JsonIgnore]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}

public class Source : BaseEntity
{
    public int SourceId { get; set; }
    public string SourceURL { get; set; }

    [JsonIgnore]
    public List<Activity> Activities { get; set; }
}

public class Connection : BaseEntity
{
    public int ConnectionId { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<Activity> Activities { get; set; }
}

[Index(nameof(HolderName), IsUnique = true)]
public class Holder : BaseEntity
{
    public int HolderId { get; set; }
    public string HolderName { get; set; }

    [JsonIgnore]
    public List<Activity> Activities { get; set; }
}

public class Branch : BaseEntity
{
    public int BranchId { get; set; }
    public string BranchName { get; set; }

    public List<ApplyStart> ApplyStart { get; set;  }
    public List<ApplyEnd> ApplyEnd { get; set;  }
    public List<ApplyFee> ApplyFee { get; set;  }
    public List<DateStart> DateStart { get; set; }
    public List<DateEnd> DateEnd { get; set; }
    public List<Location> Locations { get; set; }

    [JsonIgnore]
    public List<BranchStatus> BranchStatus { get; set; }
}

public class BranchStatus : BaseEntity
{
    public int BranchStatusId { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(20)")]
    public string Status { get; set; }
}

public class ApplyStart : BaseEntity
{
    public int ApplyStartId { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class ApplyEnd : BaseEntity
{
    public int ApplyEndId { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class ApplyFee : BaseEntity
{
    public int ApplyFeeId { get; set; }
    public string Fee { get; set; }

    [JsonIgnore]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class DateStart : BaseEntity
{
    public int DateStartId { get; set; }
    public string Name { get; set; }
    public string Date { get; set; }

    [JsonIgnore]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class DateEnd : BaseEntity
{
    public int DateEndId { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

[Index(nameof(Content), IsUnique = true)]
public class Location
{
    public int LocationId { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<Branch>? Branches { get; set; }
}

