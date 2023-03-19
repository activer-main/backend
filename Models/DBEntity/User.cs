using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ActiverWebAPI.Models.DBEntity;

[Index(nameof(Email), IsUnique = true)]
public class User : BaseEntity
{
    [Key]
    public Guid UserId { get; set; }

    [Required]
    [Column(TypeName = "varchar(512)")]
    public string Email { get; set; }

    public bool Verified { get; set; } = false;

    [MaxLength(124)]
    public string? NickName { get; set; }

    [MaxLength(124)]
    public string? RealName { get; set; }

    [Required]
    [Column(TypeName = "char(62)")]
    public string HashedPassword { get; set; }

    [Column(TypeName = "varchar(64)")]
    public string? Phone { get; set; }
    public DateTime? BrithDay { get; set; }

    public Avatar? Avatar { get; set; }
    public int AvatarId { get; set; }

    public Area? Area { get; set; }
    public int AreaId { get; set; }

    public List<Profession>? Professions { get; set; }

    public Gender? Gender { get; set; }
    public int GenderId { get; set; }

    public List<SearchHistory>? SearchHistory { get; set; }

    public List<Objective>? ObjectiveTags { get; set; }

    public List<Comment>? Comments { get; set; }

    public List<Tag>? TagStorage { get; set; }

    public List<UserVoteTagInActivity>? UserVoteTagInActivities { get; set; }

    public List<BranchStatus>? BranchStatus { get; set; }
    
    public List<UserActivityRecord>? UserActivityRecords { get; set; }
}

public class Avatar : BaseEntity
{
    [Key]
    public int AvatarId { get; set;}
    public long Length { get; set; }
    public string Filename { get; set; }

    [Column(TypeName = "varchar(32)")]
    public string FileType { get; set; }

    public byte[] FileByte { get; set; }
}

public class Area : BaseEntity
{
    [Key]
    public int AreaId { get; set;}
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

public class County
{
    public int CountyId { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

public class Profession : BaseEntity
{
    [Key]
    public int ProfessionId { get; set; }
    [Column(TypeName = "nvarchar(124)")]
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? User { get; set; }
}

public class Gender : BaseEntity
{
    [Key]
    public int GenderId { get; set; }
    [Column(TypeName = "nvarchar(256)")]
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

public class SearchHistory : BaseEntity
{
    [Key]
    public int SearchHistoryId { get; set; }
    public string? Keyword { get; set; }
    public List<Tag>? Tags { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public Guid UserId { get; set; }
}

[Index(nameof(ObjectiveName), IsUnique = true)]
public class Objective : BaseEntity
{
    public int ObjectiveId { get; set; }

    [Column(TypeName = "nvarchar(256)")]
    public string ObjectiveName { get; set; }

    [JsonIgnore]
    public List<Activity>? Activities { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

[Index(nameof(UserId), nameof(ActivityId), IsUnique = true)]
public class Comment : BaseEntity
{
    public int CommentId { get; set; }
    [Range(0, 50)]
    public int Rate { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}

[Index(nameof(UserId), nameof(TagId), nameof(ActivityId), IsUnique = true)]
public class UserVoteTagInActivity : BaseEntity
{
    [Key]
    public int UserVoteId { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    public Tag Tag { get; set; }
    public int TagId { get; set; }

    [JsonIgnore]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }

    [Range(-1, 1)]
    public int Vote { get; set; } = 0;
}

[Index(nameof(UserId), nameof(ActivityId), IsUnique = true)]
public class UserActivityRecord : BaseEntity
{
    [Key]
    public Guid ActivityRecordId { get; set; }

    [Column(TypeName = "text")]
    public string Content { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public int UserId { get; set; }

    [JsonIgnore]
    public Activity Activity { get; set; }
    public int ActivityId { get; set; }
}