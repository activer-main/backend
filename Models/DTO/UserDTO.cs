namespace ActiverWebAPI.Models.DTO;

public class UserInfoDTO
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public bool? EmailVerified { get; set; }
    public string? Username { get; set; }
    public string? Avatar { get; set; }
    public string Gender { get; set; }
    public string? Birthday { get; set; }
    public List<UserProfessionDTO>? Professions { get; set; }
    public string? Phone { get; set; }
    public string? County { get; set; }
    public string? Area { get; set; }
}

public class UserProfessionDTO
{
    public int Id { get; set; }
    public string Profession { get; set; }
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
    public string Username { get; set; }
    public string Password { get; set; }
}

public class UserSignInDTO
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserDTO
{
    public UserInfoDTO User { get; set; }
    public TokenDTO Token { get; set; }
}

public class UserUpdateDTO
{
    public string? Username { get; set; }
    public string? Gender { get; set; }
    public string? Birthday { get; set; }
    public List<string>? Professions { get; set; }
    public string? Phone { get; set; }
    public CountyUpdateDTO? County { get; set; }
}

public class CountyDTO
{
    public int Id { get; set; }
    public string CityName { get; set; }
    public string CityEngName { get; set; }
}

public class AreaDTO
{
    public int Id { get; set; }
    public string ZipCode { get; set; }
    public string AreaName { get; set; }
    public string AreaEngName { get; set; }
}

public class CountyPostDTO
{
    public string CityName { get; set; }
    public string CityEngName { get; set; }
    public List<AreaPostDTO> Areas { get; set;}
}

public class AreaPostDTO
{
    public string ZipCode { get; set; }
    public string AreaName { get; set; }
    public string AreaEngName { get; set; }
}

public class CountyUpdateDTO
{
    public string CityName { get; set; }
    public string CityEngName { get; set; }
    public AreaUpdateDTO Area { get; set; }
}

public class AreaUpdateDTO
{
    public string ZipCode { get; set; }
    public string AreaName { get; set; }
    public string AreaEngName { get; set; }
}