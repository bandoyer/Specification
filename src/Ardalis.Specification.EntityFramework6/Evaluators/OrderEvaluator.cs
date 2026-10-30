using System.Collections.Concurrent;
using System.Reflection;

namespace Ardalis.Specification.EntityFramework6;

public class OrderEvaluator : IEvaluator, IInMemoryEvaluator
{
    private static readonly MethodInfo _orderByMethodInfo = GetQueryableMethod(nameof(Queryable.OrderBy));
    private static readonly MethodInfo _orderByDescendingMethodInfo = GetQueryableMethod(nameof(Queryable.OrderByDescending));
    private static readonly MethodInfo _thenByMethodInfo = GetQueryableMethod(nameof(Queryable.ThenBy));
    private static readonly MethodInfo _thenByDescendingMethodInfo = GetQueryableMethod(nameof(Queryable.ThenByDescending));

    // Not readonly: a readonly record struct needs IsExternalInit, which net472 lacks.
    private record struct CacheKey(Type EntityType, Type KeyType, OrderTypeEnum OrderType);
    private static readonly ConcurrentDictionary<CacheKey, Func<IQueryable, LambdaExpression, IQueryable>> _cache = new();

    private OrderEvaluator() { }
    public static OrderEvaluator Instance { get; } = new OrderEvaluator();

    public bool IsCriteriaEvaluator { get; } = false;

    public IQueryable<T> GetQuery<T>(IQueryable<T> query, ISpecification<T> specification) where T : class
    {
        if (specification is Specification<T> spec)
        {
            if (spec.OneOrManyOrderExpressions.IsEmpty) return query;
            if (spec.OneOrManyOrderExpressions.SingleOrDefault is { } orderExpression)
            {
                return orderExpression.OrderType switch
                {
                    OrderTypeEnum.OrderBy or OrderTypeEnum.OrderByDescending => Order(query, orderExpression),
                    _ => query
                };
            }
        }

        IQueryable<T>? orderedQuery = null;
        var chainCount = 0;
        foreach (var orderExpression in specification.OrderExpressions)
        {
            if (orderExpression.OrderType is OrderTypeEnum.OrderBy or OrderTypeEnum.OrderByDescending)
            {
                chainCount++;
                if (chainCount == 2) throw new DuplicateOrderChainException();
                orderedQuery = Order(query, orderExpression);
            }
            else if (orderExpression.OrderType is OrderTypeEnum.ThenBy or OrderTypeEnum.ThenByDescending)
            {
                orderedQuery = Order(orderedQuery!, orderExpression);
            }
        }

        if (orderedQuery is not null)
        {
            query = orderedQuery;
        }

        return query;
    }

    // In memory there is no Convert to remove, so the core evaluator sorts with the compiled key selector.
    public IEnumerable<T> Evaluate<T>(IEnumerable<T> query, ISpecification<T> specification)
        => Ardalis.Specification.OrderEvaluator.Instance.Evaluate(query, specification);

    private static IQueryable<T> Order<T>(IQueryable<T> query, OrderExpressionInfo<T> orderExpression)
    {
        var keySelector = RemoveConvert(orderExpression.KeySelector);
        var key = new CacheKey(typeof(T), keySelector.ReturnType, orderExpression.OrderType);
        var order = _cache.GetOrAdd(key, CreateOrderDelegate);
        return (IQueryable<T>)order(query, keySelector);
    }

    // EF6 can't translate the Convert to object that a value-type key selector holds, so the key keeps its own type.
    private static LambdaExpression RemoveConvert(LambdaExpression source)
    {
        var body = source.Body;
        if (body.NodeType != ExpressionType.Convert) return source;

        while (body.NodeType == ExpressionType.Convert)
            body = ((UnaryExpression)body).Operand;

        return Expression.Lambda(body, source.Parameters);
    }

    private static Func<IQueryable, LambdaExpression, IQueryable> CreateOrderDelegate(CacheKey cacheKey)
    {
        var methodInfo = cacheKey.OrderType switch
        {
            OrderTypeEnum.OrderBy => _orderByMethodInfo,
            OrderTypeEnum.OrderByDescending => _orderByDescendingMethodInfo,
            OrderTypeEnum.ThenBy => _thenByMethodInfo,
            _ => _thenByDescendingMethodInfo
        };

        var orderMethod = methodInfo.MakeGenericMethod(cacheKey.EntityType, cacheKey.KeyType);
        var parameters = orderMethod.GetParameters();
        var sourceParameter = Expression.Parameter(typeof(IQueryable));
        var selectorParameter = Expression.Parameter(typeof(LambdaExpression));

        var call = Expression.Call(
            orderMethod,
            Expression.Convert(sourceParameter, parameters[0].ParameterType),
            Expression.Convert(selectorParameter, parameters[1].ParameterType));

        var lambda = Expression.Lambda<Func<IQueryable, LambdaExpression, IQueryable>>(call, sourceParameter, selectorParameter);
        return lambda.Compile();
    }

    private static MethodInfo GetQueryableMethod(string name) => typeof(Queryable)
        .GetTypeInfo().GetDeclaredMethods(name)
        .Single(mi => mi.GetParameters().Length == 2);
}
