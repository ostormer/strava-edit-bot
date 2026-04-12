using StravaAPILibary.Models.Athletes;
using System;

namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents a comment made on an activity.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// The unique identifier of this comment.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The identifier of the activity this comment is related to.
        /// </summary>
        public long ActivityId { get; set; }

        /// <summary>
        /// The content of the comment.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The athlete who made the comment.
        /// </summary>
        public SummaryAthlete Athlete { get; set; } = new SummaryAthlete();

        /// <summary>
        /// The time at which this comment was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
