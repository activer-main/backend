using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Services.TagServices;

public class TagService : GenericService<Tag, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Tag, int> _tagRepository;

    public TagService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _tagRepository = _unitOfWork.Repository<Tag, int>();
    }

    public Tag? GetTagByTextType(string text, string type)
    {
        var tag = _tagRepository.Query().FirstOrDefault(x => x.Text == text && x.Type == type);
        return tag;
    }

    public async Task<Tag?> GetTagByTextAsync(string text)
    {
        var tag = await _tagRepository.Query().FirstOrDefaultAsync(x => x.Text == text);
        return tag;
    }

    public Tag? GetTagByText(string text)
    {
        var tag = _tagRepository.Query().FirstOrDefault(x => x.Text == text);
        return tag;
    }

    public void AddTagTrendCount(Tag tag, int count = 1)
    {
        tag.TagClickCount += count;
    }

}
