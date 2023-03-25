using ActiverWebAPI.Interfaces.Repository;
namespace ActiverWebAPI.Models.DBEntity;

public class BaseEntity 
{ 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}