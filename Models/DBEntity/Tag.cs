using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ActiverWebAPI.Models.DBEntity;

[Index(nameof(Text), nameof(Type), IsUnique = true)]
public class Tag : BaseEntity
{
    public int TagId { get; set; }

    [Column(TypeName="nvarchar(50)")]
    public string Text { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string Type { get; set; }
    public int TagClickCount { get; set; } = 0;

    [JsonIgnore]
    public List<Activity>? Activities { get; set; }

    [JsonIgnore]
    public List<User>? Users { get; set; }

    [JsonIgnore]
    public List<SearchHistory>? SearchHistory { get; set; }

    [JsonIgnore]
    public List<UserVoteTagInActivity>? UserVoteTagInActivity { get; set; }
}
