using System;
using System.Text.Json.Serialization;

namespace StravaAPILibary.Models.Athletes
{
    /// <summary>
    /// Represents a summary of an athlete with profile and location details.
    /// </summary>
    public class SummaryAthlete
    {
        /// <summary>
        /// The unique identifier of the athlete.
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// Resource state, indicates level of detail.
        /// Possible values: 1 (meta), 2 (summary), 3 (detail).
        /// </summary>
        [JsonPropertyName("resource_state")]
        public int ResourceState { get; set; }

        /// <summary>
        /// The athlete's first name.
        /// </summary>
        [JsonPropertyName("firstname")]
        public string Firstname { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's last name.
        /// </summary>
        [JsonPropertyName("lastname")]
        public string Lastname { get; set; } = string.Empty;

        /// <summary>
        /// URL to a 62x62 pixel profile picture.
        /// </summary>
        [JsonPropertyName("profile_medium")]
        public string ProfileMedium { get; set; } = string.Empty;

        /// <summary>
        /// URL to a 124x124 pixel profile picture.
        /// </summary>
        [JsonPropertyName("profile")]
        public string Profile { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's city.
        /// </summary>
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's state or geographical region.
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's country.
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's sex. M = Male, F = Female.
        /// </summary>
        [JsonPropertyName("sex")]
        public string Sex { get; set; } = string.Empty;

        /// <summary>
        /// Deprecated. Use Summit field instead. Whether the athlete has any Summit subscription.
        /// </summary>
        [JsonPropertyName("premium")]
        public bool Premium { get; set; }

        /// <summary>
        /// Whether the athlete has any Summit subscription.
        /// </summary>
        [JsonPropertyName("summit")]
        public bool Summit { get; set; }

        /// <summary>
        /// The time at which the athlete was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The time at which the athlete was last updated.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
