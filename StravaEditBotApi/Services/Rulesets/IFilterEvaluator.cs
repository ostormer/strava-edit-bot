using StravaAPILibrary.Models.Activities;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services.Rulesets;

public interface IFilterEvaluator
{
    bool Evaluate(FilterExpression filter, DetailedActivity activity);
}
