using ActiverWebAPI.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ActiverWebAPI.Models.DBEntity;

[Index(nameof(Email), IsUnique = true)]
public class User : BaseEntity, IEntity<Guid>
{
    [Key]
    public Guid Id { get; set; }
    public int UserRole { get; set; } = (int) Enum.UserRole.User;

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

public class Avatar : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set;}
    public long Length { get; set; }
    public string Filename { get; set; }

    [Column(TypeName = "varchar(32)")]
    public string FileType { get; set; }
    public string FilePath { get; set; }
}

public class Area : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set;}
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

public class County: BaseEntity, IEntity<int>
{
    public int Id { get; set; }
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

public class Profession : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }
    [Column(TypeName = "nvarchar(124)")]
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? User { get; set; }
}

public class Gender : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }
    [Column(TypeName = "nvarchar(256)")]
    public string Content { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

public class SearchHistory : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }
    public string? Keyword { get; set; }
    public List<Tag>? Tags { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public Guid UserId { get; set; }
}

[Index(nameof(ObjectiveName), IsUnique = true)]
public class Objective : BaseEntity, IEntity<int>
{
    public int Id { get; set; }

    [Column(TypeName = "nvarchar(256)")]
    public string ObjectiveName { get; set; }

    [JsonIgnore]
    public List<Activity>? Activities { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }
}

[Index(nameof(UserId), nameof(ActivityId), IsUnique = true)]
public class Comment : BaseEntity, IEntity<int>
{
    public int Id { get; set; }
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
public class UserVoteTagInActivity : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }

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
public class UserActivityRecord : BaseEntity, IEntity<Guid>
{
    [Key]
    public Guid Id { get; set; }

    [Column(TypeName = "text")]
    public string Content { get; set; }

    [JsonIgnore]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}