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
    public int UserRole { get; set; } = (int) Enums.UserRole.User;
   
    [Column(TypeName = "varchar(512)")]
    public string Email { get; set; }

    public bool Verified { get; set; } = false;

    [MaxLength(124)]
    public string? NickName { get; set; }

    [MaxLength(124)]
    public string? RealName { get; set; }

    public int Gender { get; set; } = (int) Enums.UserGender.Undefined;

    [Required]
    public string HashedPassword { get; set; }

    [Column(TypeName = "varchar(64)")]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BrithDay { get; set; }

    public Avatar? Avatar { get; set; }
    public int? AvatarId { get; set; }

    public Area? Area { get; set; }
    public int? AreaId { get; set; }

    public List<Profession>? Professions { get; set; }

    public County? County { get; set; }
    public int? CountyId { get; set; }

    public List<SearchHistory>? SearchHistory { get; set; } = new List<SearchHistory> { };

    public List<Objective>? ObjectiveTags { get; set; } = new List<Objective> { };

    public List<Comment>? Comments { get; set; } = new List<Comment> { };

    public List<Tag>? TagStorage { get; set; } = new List<Tag> { };

    public List<UserVoteTagInActivity>? UserVoteTagInActivities { get; set; } = new List<UserVoteTagInActivity> { };

    public List<ActivityStatus>? ActivityStatus { get; set; } = new List<ActivityStatus> { };
    
    public List<UserActivityRecord>? UserActivityRecords { get; set; } = new List<UserActivityRecord> { };
    public List<UserEmailVerification>? UserEmailVerifications { get; set; } = new List<UserEmailVerification>();
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

    [JsonIgnore]
    [Required]
    public User User { get; set; }
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

public class SearchHistory : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }
    public string? Keyword { get; set; }
    public List<Tag>? Tags { get; set; }

    [JsonIgnore]
    [Required]
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
    [Required]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}

[Index(nameof(UserId), nameof(TagId), nameof(ActivityId), IsUnique = true)]
public class UserVoteTagInActivity : BaseEntity, IEntity<int>
{
    [Key]
    public int Id { get; set; }

    [JsonIgnore]
    [Required]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    [Required]
    public Tag Tag { get; set; }
    public int TagId { get; set; }

    [JsonIgnore]
    [Required]
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
    [Required]
    public User User { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore]
    [Required]
    public Activity Activity { get; set; }
    public Guid ActivityId { get; set; }
}

[Index(nameof(UserId), nameof(VerificationCode), IsUnique = true)]
public class UserEmailVerification
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    [Required]
    public User User { get; set; }

    [Column(TypeName = "char(6)")]
    public string VerificationCode { get; set; }
    public DateTime ExpiresTime { get; set; }
}