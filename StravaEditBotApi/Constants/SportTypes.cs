namespace StravaEditBotApi.Constants;

public static class SportTypes
{
    public static readonly HashSet<string> Valid =
        ["Run", "Ride", "Swim", "Walk", "Hike", "WeightTraining"];

    public static string FormattedList => string.Join(", ", Valid);
}