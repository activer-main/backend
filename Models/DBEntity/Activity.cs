using ActiverWebAPI.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ActiverWebAPI.Models.DBEntity;

public class Activity : BaseEntity, IEntity<Guid>
{
    public Guid Id { get; set; }
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
    public List<Tag>? Tags { get; set; }
    public List<UserVoteTagInActivity>? UserVoteTagInActivity { get; set; }
    public List<UserActivityRecord>? UserActivityRecords { get; set; }
}

public class Image : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string ImageURL { get; set; }

    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}

public class Source : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string SourceURL { get; set; }

    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}

[Index(nameof(Content), IsUnique = true)]
public class Connection : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<Activity> Activities { get; set; }
}

[Index(nameof(HolderName), IsUnique = true)]
public class Holder : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string HolderName { get; set; }

    [JsonIgnore]
    public List<Activity> Activities { get; set; }
}

public class Branch : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string BranchName { get; set; }

    public List<ApplyStart> ApplyStart { get; set;  }
    public List<ApplyEnd> ApplyEnd { get; set;  }
    public List<ApplyFee> ApplyFee { get; set;  }
    public List<DateStart> DateStart { get; set; }
    public List<DateEnd> DateEnd { get; set; }
    public List<Location> Locations { get; set; }

    public Guid ActivityId { get; set; }
    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }

    [JsonIgnore]
    public List<BranchStatus> BranchStatus { get; set; }
}

public class BranchStatus : BaseEntity, IEntity<int>
{
    public int Id { get; set; }

    [JsonIgnore]
    [Required]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    [Required]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string Status { get; set; }
}

public class ApplyStart : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    [Required]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class ApplyEnd : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    [Required]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class ApplyFee : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Fee { get; set; }

    [JsonIgnore]
    [Required]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class DateStart : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Date { get; set; }

    [JsonIgnore]
    [Required]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

public class DateEnd : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    [Required]
    public Branch Branch { get; set; }
    public int BranchId { get; set; }
}

[Index(nameof(Content), IsUnique = true)]
public class Location : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<Branch>? Branches { get; set; }
}
