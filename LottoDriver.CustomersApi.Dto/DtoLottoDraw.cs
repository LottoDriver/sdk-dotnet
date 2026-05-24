using System;
using System.Collections.Generic;

namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// A specific event of lottery draw round.
    /// </summary>
    public class DtoLottoDraw
    {
        /// <summary>
        /// LottoDriver's identification number for the specific lottery draw.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Lottery type
        /// </summary>
        public DtoLotto Lotto { get; private set; }

        /// <summary>
        /// The time when the draw round was originally scheduled.
        /// </summary>
        public DateTime ScheduledTimeUtc { get; set; }

        /// <summary>
        /// The time when the draw actually happened. Before the
        /// result is known, this is the same as the ScheduledTimeUtc.
        /// </summary>
        public DateTime DrawTimeUtc { get; set; }

        /// <summary>
        /// The recommended time after which the bets on this round
        /// should be forbidden.
        /// </summary>
        public DateTime RecommendedClosingTimeUtc { get; set; }

        /// <summary>
        /// Status of the draw. The status is very important for bets resulting
        /// (i.e. marking the bets as winning or losing).
        ///
        /// Enum type is not used here to allow adding statuses without breaking the SDK.
        /// 
        /// Clients should use the extension method: <see cref="DtoLottoDrawExtensions.GetStatusType"/>.
        ///
        /// See <see cref="DtoLottoDrawStatus"/> for more information.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// List of numbers drawn for the round.
        /// The list is empty if the result is not known yet.
        /// </summary>
        public List<int> Result { get; set; }

        /// <summary>
        /// Extra result groups (e.g. golden or bonus balls) keyed by group name 
        /// with the drawn numbers per group. Null if no such result exists.
        /// </summary>
        public Dictionary<string, int[]> ExtraResult { get; set; }

        internal void SetLotto(DtoLotto lotto)
        {
            Lotto = lotto;
        }
    }

    /// <summary>
    /// Extension helper methods for DtoLottoDraw
    /// </summary>
    public static class DtoLottoDrawExtensions
    {
        /// <summary>
        /// Returns the draw status converted to <see cref="DtoLottoDrawStatus"/>.
        /// </summary>
        /// <param name="draw"></param>
        /// <returns></returns>
        public static DtoLottoDrawStatus GetStatusType(this DtoLottoDraw draw)
        {
            if (draw == null) return DtoLottoDrawStatus.Unknown;

            return Enum.IsDefined(typeof(DtoLottoDrawStatus), draw.Status)
                ? (DtoLottoDrawStatus) draw.Status
                : DtoLottoDrawStatus.Unknown;
        }
    }
}
