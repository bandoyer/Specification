using System.Data.Entity;
using System.Diagnostics;
using System.Reflection;

namespace OpenSpecification.EntityFramework6;

public static class SearchExtension
{
    // EF6 sends a property of a constant object as a parameter, the way it sends a captured variable.
    // A bare constant it writes into the SQL as a literal, so each search term would be a new SQL text.
    private sealed class StringVar(string value)
    {
        public string Value { get; } = value;
    }
    private static readonly PropertyInfo _stringVarValueProperty = typeof(StringVar).GetProperty(nameof(StringVar.Value))!;
    private static readonly MethodInfo _likeMethodInfo = typeof(DbFunctions)
        .GetMethod(nameof(DbFunctions.Like), [typeof(string), typeof(string)])!;

    private static MemberExpression StringAsExpression(string value)
        => Expression.Property(Expression.Constant(new StringVar(value)), _stringVarValueProperty);

    public static IQueryable<T> ApplySingleLike<T>(this IQueryable<T> source, SearchExpressionInfo<T> searchExpression)
    {
        Debug.Assert(_likeMethodInfo is not null);

        var param = searchExpression.Selector.Parameters[0];
        var selectorExpr = searchExpression.Selector.Body;
        var patternExpr = StringAsExpression(searchExpression.SearchTerm);

        var likeExpr = Expression.Call(
            null,
            _likeMethodInfo,
            selectorExpr,
            patternExpr);

        return source.Where(Expression.Lambda<Func<T, bool>>(likeExpr, param));
    }

    public static IQueryable<T> ApplyLikesAsOrGroup<T>(this IQueryable<T> source, IEnumerable<SearchExpressionInfo<T>> searchExpressions)
    {
        Debug.Assert(_likeMethodInfo is not null);

        Expression? combinedExpr = null;
        ParameterExpression? mainParam = null;
        ParameterReplacerVisitor? visitor = null;

        foreach (var searchExpression in searchExpressions)
        {
            ApplyLikeAsOrGroup(ref combinedExpr, ref mainParam, ref visitor, searchExpression);
        }

        return combinedExpr is null || mainParam is null
            ? source
            : source.Where(Expression.Lambda<Func<T, bool>>(combinedExpr, mainParam));
    }

    // The span overload's stand-in, for SearchEvaluator's sorted list: the group is the items from index from up to to.
    internal static IQueryable<T> ApplyLikesAsOrGroup<T>(this IQueryable<T> source, List<SearchExpressionInfo<T>> searchExpressions, int from, int to)
    {
        Debug.Assert(_likeMethodInfo is not null);

        Expression? combinedExpr = null;
        ParameterExpression? mainParam = null;
        ParameterReplacerVisitor? visitor = null;

        for (var i = from; i < to; i++)
        {
            ApplyLikeAsOrGroup(ref combinedExpr, ref mainParam, ref visitor, searchExpressions[i]);
        }

        return combinedExpr is null || mainParam is null
            ? source
            : source.Where(Expression.Lambda<Func<T, bool>>(combinedExpr, mainParam));
    }

    private static void ApplyLikeAsOrGroup<T>(
        ref Expression? combinedExpr,
        ref ParameterExpression? mainParam,
        ref ParameterReplacerVisitor? visitor,
        SearchExpressionInfo<T> searchExpression)
    {
        mainParam ??= searchExpression.Selector.Parameters[0];

        var selectorExpr = searchExpression.Selector.Body;
        if (mainParam != searchExpression.Selector.Parameters[0])
        {
            visitor ??= new ParameterReplacerVisitor(searchExpression.Selector.Parameters[0], mainParam);

            // If there are more than 2 search items, we want to avoid creating a new visitor instance (saving 32 bytes per instance).
            // We're in a sequential loop, no concurrency issues.
            visitor.Update(searchExpression.Selector.Parameters[0], mainParam);
            selectorExpr = visitor.Visit(selectorExpr);
        }

        var patternExpr = StringAsExpression(searchExpression.SearchTerm);

        var likeExpr = Expression.Call(
            null,
            _likeMethodInfo,
            selectorExpr,
            patternExpr);

        combinedExpr = combinedExpr is null
            ? likeExpr
            : Expression.OrElse(combinedExpr, likeExpr);
    }
}
