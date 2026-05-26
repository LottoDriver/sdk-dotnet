using System;

namespace LottoDriver.Examples.CustomersApi.Common
{
    /// <summary>
    /// A draw as stored in the example app's local SQLite database. Mirrors
    /// <c>LottoDriver.CustomersApi.Dto.DtoLottoDraw</c>, with the betting company's
    /// own ids and serialized result strings suitable for SQLite storage.
    /// </summary>
    public class LottoDraw
    {
        /// <summary>
        /// Local primary key. Autoincrement; <c>0</c> means "not yet inserted".
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to <see cref="Lotto.Id"/> in the local <c>lotto</c> table.
        /// This is the local id, not LottoDriver's lottery id.
        /// </summary>
        public int LottoId { get; set; }

        /// <summary>
        /// Originally scheduled time, in UTC. Does not change after the row is
        /// created; reschedules surface on <see cref="DrawTimeUtc"/>.
        /// </summary>
        public DateTime ScheduledTimeUtc { get; set; }

        /// <summary>
        /// Actual draw time, in UTC. Equal to <see cref="ScheduledTimeUtc"/> on
        /// initial publication; updated when LottoDriver detects a schedule change.
        /// </summary>
        public DateTime DrawTimeUtc { get; set; }

        /// <summary>
        /// Current lifecycle status. See <see cref="LottoDrawStatus"/>.
        /// </summary>
        public LottoDrawStatus Status { get; set; }

        /// <summary>
        /// Comma-separated drawn numbers in draw order, for example
        /// <c>"7,12,33,45"</c>. <c>null</c> until the result is known.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// JSON-serialized <c>Dictionary&lt;string, int[]&gt;</c> holding extra-ball
        /// groups (bonus, golden, etc.), keyed by group name. <c>null</c> if the
        /// lottery has no extra balls or none have been drawn yet.
        /// </summary>
        public string ExtraResult { get; set; }

        /// <summary>
        /// LottoDriver's draw id, if this row originated from the LottoDriver feed.
        /// Nullable for the same reason as <see cref="Lotto.LottoDriverLottoId"/>.
        /// </summary>
        public long? LottoDriverDrawId { get; set; }

        /// <summary>
        /// LottoDriver's recommended betting cutoff, in UTC. The example apps
        /// honour this conservatively; an integrator is free to use a stricter
        /// cutoff tailored per lottery.
        /// </summary>
        public DateTime RecommendedClosingTimeUtc { get; set; }

        /// <summary>
        /// True when betting is currently permitted: the draw is <see cref="LottoDrawStatus.Published"/>
        /// and the recommended cutoff has not yet passed.
        /// </summary>
        public bool IsBettingAllowed => Status == LottoDrawStatus.Published
                                        && DateTime.UtcNow < RecommendedClosingTimeUtc;
    }
}
