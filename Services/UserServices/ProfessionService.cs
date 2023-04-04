using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Services.UserServices;

public class ProfessionService : GenericService<Profession, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Profession, int> _professionRepository;

    public ProfessionService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _professionRepository = _unitOfWork.Repository<Profession, int>();
    }

    public async Task<Profession?> GetByNameAsync(string name)
    {
        var query = _professionRepository.Query();
        return await query.FirstOrDefaultAsync(e => e.Content == name);
    }

    //public async Task<IQueryable<Profession>?> GetByNamesAsync(string names)
    //{
    //    List<string> professions = names.Split('/').ToList();
    //    var query = _professionRepository.Query();
    //    query.Where(x => x.Content = );
    //}
}
