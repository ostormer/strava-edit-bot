using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services;

public interface IRulesetValidator
{
    RulesetValidationResult Validate(FilterExpression? filter, RulesetEffect? effect);
}
