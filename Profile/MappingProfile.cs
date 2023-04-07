namespace ActiverWebAPI.Profile;

using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using AutoMapper;
using ActiverWebAPI.Interfaces.Service;
using Microsoft.Extensions.Configuration;
using ActiverWebAPI.Enums;
using ActiverWebAPI.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;

public class MappingProfile : Profile
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

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
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.UserVoteTagInActivity == null ? null : src.UserVoteTagInActivity.Select(x => new TagDTO { Text = x.Tag.Text, Type = x.Tag.Type, Trend = x.Tag.TagClickCount, UserVoted = false, ActivityAmount = x.Tag.Activities == null ? 0 : x.Tag.Activities.Count }).Distinct()))
            .ForMember(dest => dest.Branches, opt => opt.MapFrom(src => src.Branches));
        CreateMap<Branch, BranchDTO>()
            .ForMember(dest => dest.DateStart, opt => opt.MapFrom(src => src.DateStart == null ? null : src.DateStart.Select(x => new KeyValuePair<string, string>(x.Name, x.Date))))
            .ForMember(dest => dest.DateEnd, opt => opt.MapFrom(src => src.DateEnd == null ? null : src.DateEnd.Select(x => x.Content)))
            .ForMember(dest => dest.ApplyStart, opt => opt.MapFrom(src => src.ApplyStart == null ? null : src.ApplyStart.Select(x => x.Content)))
            .ForMember(dest => dest.ApplyEnd, opt => opt.MapFrom(src => src.ApplyEnd == null ? null : src.ApplyEnd.Select(x => x.Content)))
            .ForMember(dest => dest.ApplyFee, opt => opt.MapFrom(src => src.ApplyFee == null ? null : src.ApplyFee.Select(x => x.Fee)))
            .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.Locations == null ? null : src.Locations.Select(x => x.Content)))
            ;
    }

    public MappingProfile(
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        IUnitOfWork unitOfWork
        ) : this()
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;

        CreateMap<UserSignUpDTO, User>()
            .ForMember(dest => dest.HashedPassword, opt => opt.MapFrom(src => passwordHasher.HashPassword(src.Password)));
        CreateMap<User, UserInfoDTO>()
            .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar == null ? null : _configuration["Server:Domain"] + $"api/user/avatar/{src.Avatar.Id}"))
            .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Area == null ? null : src.Area.Content))
            .ForMember(dest => dest.Professions, opt => opt.MapFrom(src => src.Professions == null ? null : src.Professions.Select(x => new UserProfessionDTO { Id = x.Id, Profession = x.Content }).ToList()))
            .ForMember(dest => dest.County, opt => opt.MapFrom(src => src.County == null ? null : src.County.Content))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.NickName))
            .ForMember(dest => dest.EmailVerifed, opt => opt.MapFrom(src => src.Verified))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => ((UserGender)src.Gender).ToString()))
            .ForMember(dest => dest.Birthday, opt => opt.MapFrom(src => src.BrithDay == null ? null : src.BrithDay.Value.ToShortDateString()));

        CreateMap<ActivityPostDTO, Activity>()
                        .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images == null ? null : src.Images.Select(x => new Image { ImageURL = x })))
                        .ForMember(dest => dest.Sources, opt => opt.MapFrom(src => src.Sources == null ? null : src.Sources.Select(x => new Source { SourceURL = x })))
                        .ForMember(dest => dest.Connections, opt => opt.MapFrom(src => src.Connections == null ? null : MapConnectionsAsync(src.Connections).Result))
                        .ForMember(dest => dest.Holders, opt => opt.MapFrom(src => src.Holders == null ? null : MapHoldersAsync(src.Holders).Result))
                        .ForMember(dest => dest.Objectives, opt => opt.MapFrom(src => src.Objectives == null ? null : MapObjectivesAsync(src.Objectives).Result))
                        .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags == null ? null : MapTagsAsync(src.Tags).Result))
                        .ForMember(dest => dest.Branches, opt => opt.MapFrom(src => src.Branches));
        CreateMap<BranchPostDTO, Branch>()
                        .ForMember(dest => dest.ApplyStart, opt => opt.MapFrom(src => src.ApplyStart.Select(x => new ApplyStart { Content = x})))
                        .ForMember(dest => dest.ApplyEnd, opt => opt.MapFrom(src => src.ApplyEnd.Select(x => new ApplyEnd { Content = x })))
                        .ForMember(dest => dest.ApplyFee, opt => opt.MapFrom(src => src.ApplyFee.Select(x => new ApplyFee { Fee = x })))
                        .ForMember(dest => dest.DateStart, opt => opt.MapFrom(src => src.DateStart.Select(x => new DateStart { Name = x.Key, Date = x.Value })))
                        .ForMember(dest => dest.DateEnd, opt => opt.MapFrom(src => src.DateEnd.Select(x => new DateEnd { Content = x })))
                        .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => MapLocationsAsync(src.Locations).Result))
                        ;
    }

    private async Task<List<Connection>> MapConnectionsAsync(IEnumerable<string> connections)
    {
        var result = new List<Connection>();

        foreach (var x in connections)
        {
            var connection = await _unitOfWork.Repository<Connection, int>()
                .GetAll(c => c.Content.Equals(x))
                .FirstOrDefaultAsync();

            if (connection == null)
                result.Add(new Connection { Content = x });
            else
                result.Add(connection);
        }

        return result;
    }

    private async Task<List<Holder>> MapHoldersAsync(IEnumerable<string> holders)
    {
        var result = new List<Holder>();

        foreach (var x in holders)
        {
            var holder = await _unitOfWork.Repository<Holder, int>()
                .GetAll(c => c.HolderName.Equals(x))
                .FirstOrDefaultAsync();

            if (holder == null)
                result.Add(new Holder { HolderName = x });
            else
                result.Add(holder);
        }

        return result;
    }

    private async Task<List<Objective>> MapObjectivesAsync(IEnumerable<string> objectives)
    {
        var result = new List<Objective>();

        foreach (var x in objectives)
        {
            var objective = await _unitOfWork.Repository<Objective, int>()
                .GetAll(c => c.ObjectiveName.Equals(x))
                .FirstOrDefaultAsync();

            if (objective == null)
                result.Add(new Objective { ObjectiveName = x });
            else
                result.Add(objective);
        }

        return result;
    }

    private async Task<List<Tag>> MapTagsAsync(IEnumerable<TagPostDTO> tags)
    {
        var result = new List<Tag>();

        foreach (var x in tags)
        {
            var tag = await _unitOfWork.Repository<Tag, int>()
                .GetAll(c => c.Text.Equals(x.Text) && c.Type.Equals(x.Type))
                .FirstOrDefaultAsync();

            if (tag == null)
                result.Add(new Tag { Text = x.Text, Type = x.Type });
            else
                result.Add(tag);
        }

        return result;
    }

    private async Task<List<Location>> MapLocationsAsync(IEnumerable<string> locations)
    {
        var result = new List<Location>();

        foreach (var x in locations)
        {
            var location = await _unitOfWork.Repository<Location, int>()
                .GetAll(c => c.Content.Equals(x))
                .FirstOrDefaultAsync();

            if (location == null)
                result.Add(new Location { Content = x });
            else
                result.Add(location);
        }

        return result;
    }

}


