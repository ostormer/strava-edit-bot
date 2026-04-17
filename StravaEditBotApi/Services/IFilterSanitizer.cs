using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services;

public interface IFilterSanitizer
{
    /// <summary>
    /// Walks the filter tree and nulls out the value on checks for PII or user-specific properties:
    /// start_location, end_location, gear_id.
    /// Returns the sanitized filter and the list of properties that were sanitized.
    /// </summary>
    (FilterExpression Sanitized, List<string> SanitizedProperties) SanitizeForSharing(FilterExpression filter);
}
