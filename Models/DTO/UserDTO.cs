namespace ActiverWebAPI.Models.DTO;

public class UserInfoDTO
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string? RealName { get; set; }
    public string? NickName { get; set; }
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Profession { get; set; }
    public string? Phone { get; set; }
    public string? County { get; set; }
    public string? Area { get; set; }
    public List<UserActivityRecordDTO>? ActivityHistory { get; set; }
    public List<TagDTO>? TagStorage { get; set; }
}

public class UserActivityRecordDTO
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public string Owner { get; set; }
    public Guid OwnerId { get; set; }
    public string ActivityName { get; set; }
    public Guid ActivityId { get; set; }
}

public class UserSignUpDTO
{
    public string Email { get; set; }
    public string NickName { get; set; }
    public string Password { get; set; }
}

public class UserSignInDTO
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserDTO
{
    public UserInfo User { get; set; }
    public TokenDTO Token { get; set; }
}