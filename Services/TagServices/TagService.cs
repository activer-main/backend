using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;

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
}
