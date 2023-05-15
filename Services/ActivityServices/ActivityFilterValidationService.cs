using ActiverWebAPI.Exceptions;

namespace ActiverWebAPI.Services.ActivityServices;

public class ActivityFilterValidationService
{
    private readonly HashSet<string> _allowSortBySet = new() { "Trend", "CreatedAt", "AddTime" };
    private readonly HashSet<string> _allowStatusSet = new() { "願望", "已註冊", "已完成" };
    public void ValidateSortBy(string sortBy)
    {
        if (!string.IsNullOrEmpty(sortBy) && !_allowSortBySet.Contains(sortBy))
        {
            throw new BadRequestException($"排序: '{sortBy}' 不在可接受的排序列表: '{string.Join(", ", _allowSortBySet)}'");
        }
    }

    public void ValidateStatus(string status)
    {
        if (!_allowStatusSet.Contains(status))
        {
            throw new BadRequestException($"活動狀態: '{status}' 不在可接受的狀態列表: '{string.Join(", ", _allowStatusSet)}'");
        }

    }

    public void ValidateStatus(IEnumerable<string> statuses)
    {
        foreach (var status in statuses)
        {
            if (!_allowStatusSet.Contains(status))
            {
                throw new BadRequestException($"活動狀態: '{status}' 不在可接受的狀態列表: '{string.Join(", ", _allowStatusSet)}'");
            }
        }
    }

    public IEnumerable<string> GetAllowSortBySet()
    {
        return _allowSortBySet.AsEnumerable();
    }

    public IEnumerable<string> GetAllowStatusSet()
    {
        return _allowStatusSet.AsEnumerable();
    }
}