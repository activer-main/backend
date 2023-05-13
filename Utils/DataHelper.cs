using System.Linq.Expressions;
using System.Reflection;

namespace ActiverWebAPI.Utils;

public static class DataHelper
{
    /// <summary>
    /// 根據指定屬性進行排序、分頁處理
    /// </summary>
    /// <typeparam name="T">泛型類型</typeparam>
    /// <param name="data">資料集合</param>
    /// <param name="properties">排序屬性</param>
    /// <param name="propertiesExpression">排序屬性表達式</param>
    /// <param name="orderBy">排序方式（升序或降序）</param>
    /// <param name="page">頁碼</param>
    /// <param name="countPerPage">每頁顯示數量</param>
    /// <param name="propertyFirst">排序屬性是否優先</param>
    /// <returns>排序分頁後的資料集合</returns>
    public static List<T> GetSortedAndPagedData<T>(IEnumerable<T> data, List<string> properties, List<Expression<Func<T, object>>> propertiesExpression, string orderBy, int page, int countPerPage, bool propertyFirst = true)
    {
        if (page < 1 || countPerPage < 1)
        {
            throw new ArgumentException("Invalid page or count per page.");
        }

        // 先檢查是否有 properties 或 propertiesExpression，如果都沒有就直接進行分頁
        if ((properties == null || properties.Count == 0) && (propertiesExpression == null || propertiesExpression.Count == 0))
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
        else if (propertiesExpression == null || propertiesExpression.Count == 0)
        {
            data = OrderByPropertiesList(data, properties, orderBy);
        }
        else if (properties == null || properties.Count == 0)
        {
            data = OrderByExpressionList(data, propertiesExpression, orderBy);
        }
        else
        {
            // 根據 propertyFirst 決定排序的順序
            if (propertyFirst)
            {
                data = OrderByPropertiesList(data, properties, orderBy);
                data = OrderByExpressionList(data, propertiesExpression, orderBy);
            }
            else
            {
                data = OrderByExpressionList(data, propertiesExpression, orderBy);
                data = OrderByPropertiesList(data, properties, orderBy);
            }
        }

        var total = data.Count();
        var totalPages = (int)Math.Ceiling((double)total / countPerPage);
        var skip = (page - 1) * countPerPage;
        var take = countPerPage;

        var pagedData = data.Skip(skip).Take(take).ToList();

        return pagedData;
    }

    private static IEnumerable<T> OrderByPropertiesList<T>(IEnumerable<T> source, List<string> properties, string orderBy)
    {
        if (properties != null && properties.Count > 0)
        {
            var orderedData = source.OrderBy(x => 0);
            foreach (var property in properties)
            {
                var propInfo = typeof(T).GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new ArgumentException($"Sorting field '{property}' not found.");
                orderedData = orderBy.ToLower() == "descending"
                    ? orderedData.ThenByDescending(x => propInfo.GetValue(x, null))
                    : orderedData.ThenBy(x => propInfo.GetValue(x, null));
            }
            return orderedData;
        }
        else
        {
            return source;
        }
    }

    public static IOrderedQueryable<T> OrderByExpressionList<T>(IEnumerable<T> source, List<Expression<Func<T, object>>> expressions, string orderBy)
    {
        if (expressions == null || !expressions.Any())
        {
            throw new ArgumentException("Expressions cannot be null or empty.");
        }

        var orderedData = source.AsQueryable().OrderBy(expressions[0]);

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
