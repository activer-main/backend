using System.Reflection;

namespace ActiverWebAPI.Utils;

public static class DataHelper
{
    public static List<T> GetSortedAndPagedData<T>(IEnumerable<T> data, string sortBy, string orderBy, int page, int countPerPage)
    {
        var propInfo = typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) 
            ?? throw new ArgumentException("Sorting field not found.");
        orderBy ??= "descending";
        var orderedData = orderBy.ToLower() == "descending"
            ? data.OrderByDescending(x => propInfo.GetValue(x, null)).ToList()
            : data.OrderBy(x => propInfo.GetValue(x, null)).ToList();

        orderedData = orderedData.Skip((page - 1) * countPerPage).Take(countPerPage).ToList();
        return orderedData;
    }

    public static bool IsImage(IFormFile file)
    {
        return file.ContentType.StartsWith("image/");
    }
}
