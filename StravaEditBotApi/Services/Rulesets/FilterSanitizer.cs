using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services.Rulesets;

public class FilterSanitizer : IFilterSanitizer
{
    private static readonly HashSet<string> _sanitizedProperties =
    [
        "start_location",
        "end_location",
        "gear_id"
    ];

    public (FilterExpression Sanitized, List<string> SanitizedProperties) SanitizeForSharing(FilterExpression filter)
    {
        var sanitized = new List<string>();
        FilterExpression result = SanitizeNode(filter, sanitized);
        return (result, sanitized);
    }

    private static FilterExpression SanitizeNode(FilterExpression node, List<string> sanitized)
    {
        switch (node)
        {
            case AndFilter and:
                List<FilterExpression> andConditions = and.Conditions
                    .Select(c => SanitizeNode(c, sanitized))
                    .ToList();
                return new AndFilter(andConditions);

            case OrFilter or:
                List<FilterExpression> orConditions = or.Conditions
                    .Select(c => SanitizeNode(c, sanitized))
                    .ToList();
                return new OrFilter(orConditions);

            case NotFilter not:
                return new NotFilter(SanitizeNode(not.Condition, sanitized));

            case CheckFilter check:
                if (check.Property is not null && _sanitizedProperties.Contains(check.Property))
                {
                    if (!sanitized.Contains(check.Property))
                    {
                        sanitized.Add(check.Property);
                    }
                    return check with { Value = null };
                }
                return check;

            default:
                return node;
        }
    }
}
