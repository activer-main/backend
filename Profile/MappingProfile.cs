namespace ActiverWebAPI.Profile;

using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using AutoMapper;
using ActiverWebAPI.Interfaces.Service;
using Microsoft.Extensions.Configuration;
using ActiverWebAPI.Enums;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserInfoDTO, UserDTO>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
        CreateMap<TokenDTO, UserDTO>()
            .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src));
        CreateMap<Activity, ActivityDTO>()
            .ForMember(dest => dest.Trend, opt => opt.MapFrom(src => src.ActivityClickedCount))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images == null ? null : src.Images.Select(x => x.ImageURL)))
            .ForMember(dest => dest.Sources, opt => opt.MapFrom(src => src.Sources == null ? null : src.Sources.Select(x => x.SourceURL)))
            .ForMember(dest => dest.Connections, opt => opt.MapFrom(src => src.Connections == null ? null : src.Connections.Select(x => x.Content)))
            .ForMember(dest => dest.Holders, opt => opt.MapFrom(src => src.Holders == null ? null : src.Holders.Select(x => x.HolderName)))
            .ForMember(dest => dest.Objectives, opt => opt.MapFrom(src => src.Objectives == null ? null : src.Objectives.Select(x => x.ObjectiveName)))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.UserVoteTagInActivity == null ? null : src.UserVoteTagInActivity.Select(x => new TagDTO { Text=x.Tag.Text, Type=x.Tag.Type, Trend=x.Tag.TagClickCount, UserVoted=false, ActivityAmount = x.Tag.Activities == null ? 0 : x.Tag.Activities.Count }).Distinct()));
    }

    public MappingProfile(IPasswordHasher passwordHasher, IConfiguration configuration) : this()
    {
        CreateMap<UserSignUpDTO, User>()
            .ForMember(dest => dest.HashedPassword, opt => opt.MapFrom(src => passwordHasher.HashPassword(src.Password)));
        CreateMap<User, UserInfoDTO>()
            .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar == null ? null : configuration["Server:Domain"] + $"api/user/avatar/{src.Avatar.Id}"))
            .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Area == null ? null : src.Area.Content))
            .ForMember(dest => dest.Professions, opt => opt.MapFrom(src => src.Professions == null ? null : src.Professions.Select(x => new UserProfessionDTO { Id = x.Id, Profession = x.Content }).ToList()))
            .ForMember(dest => dest.County, opt => opt.MapFrom(src => src.County == null ? null : src.County.Content))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.NickName))
            .ForMember(dest => dest.EmailVerifed, opt => opt.MapFrom(src => src.Verified))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => ((UserGender)src.Gender).ToString()))
            .ForMember(dest => dest.Birthday, opt => opt.MapFrom(src => src.BrithDay == null ? null : src.BrithDay.Value.ToShortDateString()));
    }
}
