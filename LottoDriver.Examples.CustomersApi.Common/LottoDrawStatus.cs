namespace LottoDriver.Examples.CustomersApi.Common
{
    /// <summary>
    /// Local copy of the LottoDriver draw status enum used by the example apps.
    /// Values match <c>LottoDriver.CustomersApi.Dto.DtoLottoDrawStatus</c>; see
    /// that type for the canonical descriptions.
    /// </summary>
    public enum LottoDrawStatus
    {
        /// <summary>
        /// Status not recognised by this build. Treat as <see cref="UndoCleared"/>.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Draw is announced. Bets are allowed only if the integrator's own cutoff
        /// also permits it.
        /// </summary>
        Published = 0,

        /// <summary>
        /// Scheduling discrepancy detected before results. Block new bets until
        /// the status returns to <see cref="Published"/> or <see cref="Cleared"/>.
        /// </summary>
        Unpublished = 1,

        /// <summary>
        /// Result is final. Bets can be settled. Void bets placed after the
        /// actual draw time.
        /// </summary>
        Cleared = 2,

        /// <summary>
        /// A previously cleared draw is in dispute. Reverse settlements where
        /// possible; otherwise freeze payouts until <see cref="Cleared"/> or
        /// <see cref="Canceled"/>.
        /// </summary>
        UndoCleared = 3,

        /// <summary>
        /// Draw did not take place. Void all bets and refund stakes.
        /// </summary>
        Canceled = 4
    }
}
