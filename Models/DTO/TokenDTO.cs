﻿namespace ActiverWebAPI.Models.DTO;

public class TokenDTO
{
    public string AccessToken { get;set; }
    public DateTime ExpireAt { get;set; }
}
