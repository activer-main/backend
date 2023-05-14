using System.Linq.Expressions;

namespace ActiverWebAPI.Utils;

public static class DataHelper
{
    /// <summary>
    /// 根據指定屬性進行排序、分頁處理
    /// </summary>
    /// <typeparam name="T">泛型類型</typeparam>
    /// <param name="data">資料集合</param>
    /// <param name="properties">排序屬性表達式集合</param>
    /// <param name="orderBy">排序方式（升序或降序）</param>
    /// <param name="page">頁碼</param>
    /// <param name="countPerPage">每頁顯示數量</param>
    /// <returns>排序分頁後的資料集合</returns>
    public static IEnumerable<T> GetSortedAndPagedData<T>(IQueryable<T> data, List<Expression<Func<T, object>>> properties, string orderBy, int page, int countPerPage)
    {
        if (page < 1 || countPerPage < 1)
        {
            throw new ArgumentException("Invalid page or count per page.");
        }

        if (properties == null || properties.Count == 0)
        {
            if (orderBy.ToLower() == "descending")
            {
                data = data.OrderByDescending(x => x);
            }
            else
            {
                data = data.OrderBy(x => x);
            }
        }
        else
        {
            data = OrderByExpressionList(data, properties, orderBy);
        }

        var total = data.Count();
        var totalPages = (int)Math.Ceiling((double)total / countPerPage);
        var skip = (page - 1) * countPerPage;
        var take = countPerPage;

        var pagedData = data.Skip(skip).Take(take).ToList();

        return pagedData;
    }

    /// <summary>
    /// 根據指定屬性進行排序
    /// </summary>
    /// <typeparam name="T">泛型類型</typeparam>
    /// <param name="source">資料來源</param>
    /// <param name="expressions">排序屬性</param>
    /// <param name="orderBy">排序方式（升序或降序）</param>
    /// <returns>排序後的資料</returns>
    private static IOrderedQueryable<T> OrderByExpressionList<T>(IQueryable<T> source, List<Expression<Func<T, object>>> expressions, string orderBy)
    {
        if (expressions == null || !expressions.Any())
        {
            throw new ArgumentException("Expressions cannot be null or empty.");
        }

        var orderedData = source.AsQueryable().OrderByDescending(expressions[0]);

        for (int i = 1; i < expressions.Count; i++)
        {
            orderedData = orderedData.ThenBy(expressions[i]);
        }

        if (!string.IsNullOrEmpty(orderBy))
        {
            orderBy = orderBy.ToLower() == "descending" ? "OrderByDescending" : "OrderBy";
            orderedData = (IOrderedQueryable<T>)orderedData.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    orderBy,
                    new[] { typeof(T), expressions[0].ReturnType },
                    orderedData.Expression,
                    Expression.Quote(expressions[0])
                )
            );
        }

        return orderedData;
    }

    public static bool IsImage(IFormFile file)
    {
        return file.ContentType.StartsWith("image/");
    }
}
