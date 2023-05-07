using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
namespace ActiverWebAPI.Services.ActivityServices;

public class BranchService : GenericService<Branch, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Branch, int> _branchRepository;

    public BranchService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _branchRepository = _unitOfWork.Repository<Branch, int>();
    }

    //public void UpdateBranchStatus(BranchStatus branchStatusToUpdate)
    //{
    //    _branchStatusRepository.Update(branchStatusToUpdate);
    //}
}
