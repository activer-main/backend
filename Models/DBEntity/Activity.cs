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

    [Column(TypeName = "text")]
    public string Content { get; set; }

    [Column(TypeName = "text")]
    public string? Html { get; set; }
    public int ActivityClickedCount { get; set; } = 0;

    public List<ActivityFee> Fee { get; set; }
    public List<ActivityStatus>? Status { get; set; }
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

public class ActivityFee : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "nvarchar(128)")]
    public string Fee { get; set; }

    public Guid ActivityId { get; set; }
    [JsonIgnore]
    public Activity Activity { get; set; }
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
    public List<BranchDate>? Date { get; set; }
    public List<Location>? Location { get; set; }

    public Guid ActivityId { get; set; }
    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }

    //[JsonIgnore]
    //public List<BranchStatus> BranchStatus { get; set; }
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

public class ActivityStatus : BaseEntity, IEntity<int>
{
    public int Id { get; set; }

    [JsonIgnore]
    [Required]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string Status { get; set; }
}

public class BranchDate : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Start { get; set; }
    public string? End { get; set; }

    public int BranchId { get; set; }
    [JsonIgnore]
    public Branch Branch { get; set; }
}

[Index(nameof(Content), IsUnique = true)]
public class Location : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<Branch>? Branches { get; set; }
}
