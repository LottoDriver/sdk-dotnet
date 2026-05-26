using System.Data;

namespace LottoDriver.Examples.CustomersApi.Common.DataAccess
{
    /// <summary>
    /// Abstraction over the example app's local persistence store. A single
    /// implementation, <see cref="SQLiteDatabase"/>, ships with the examples.
    /// <para>
    /// All find/insert/update calls must be made between a <see cref="BeginTransaction"/>
    /// and a matching <see cref="CommitTransaction"/> or <see cref="RollbackTransaction"/>.
    /// The transaction also opens the underlying connection, so calling outside a
    /// transaction is not supported.
    /// </para>
    /// </summary>
    public interface IDatabase
    {
        /// <summary>Opens the connection and starts a transaction.</summary>
        void BeginTransaction();

        /// <summary>Rolls back the current transaction and closes the connection.</summary>
        void RollbackTransaction();

        /// <summary>Commits the current transaction and closes the connection.</summary>
        void CommitTransaction();

        /// <summary>
        /// Returns the last sequence number persisted by a previous run, or
        /// <c>0</c> if none has been stored yet. Pass this to
        /// <c>ICustomersApiClient.Connect</c> on startup.
        /// </summary>
        int GetLastSeqNo();

        /// <summary>
        /// Records the last sequence number that has been successfully persisted.
        /// Should be written in the same transaction as the data it describes.
        /// </summary>
        void SetLastSeqNo(int lastSeqNo);

        /// <summary>
        /// Looks up the local <see cref="Country"/> row that was created from a
        /// LottoDriver country id. Returns <c>null</c> if no such row exists.
        /// </summary>
        Country CountryFindByLottoDriverId(string lottoDriverId);

        /// <summary>
        /// Inserts a new country. If <paramref name="c"/> has <c>Id == 0</c>,
        /// SQLite generates the id and the property is updated in-place.
        /// </summary>
        void CountryInsert(Country c);

        /// <summary>
        /// Looks up the local <see cref="Lotto"/> row by LottoDriver lottery id.
        /// </summary>
        Lotto LottoFindByLottoDriverId(int lottoDriverLottoId);

        /// <summary>Inserts a new lottery row.</summary>
        void LottoInsert(Lotto lotto);

        /// <summary>
        /// Looks up the local <see cref="LottoDraw"/> row by LottoDriver draw id.
        /// </summary>
        LottoDraw LottoDrawFindByLottoDriverId(long lottoDriverDrawId);

        /// <summary>Inserts a new draw row.</summary>
        void LottoDrawInsert(LottoDraw draw);

        /// <summary>
        /// Updates an existing draw row by <see cref="LottoDraw.Id"/>. Returns the
        /// number of rows affected.
        /// </summary>
        int LottoDrawUpdate(LottoDraw draw);

        /// <summary>
        /// Fills <paramref name="dataTable"/> with draws scheduled between six
        /// hours ago and five minutes from now. Used by the WinForms viewer.
        /// </summary>
        void LottoDrawFindRecent(DataTable dataTable);

        /// <summary>
        /// Runs idempotent schema migrations to bring the database to the latest
        /// version. Safe to call on every startup.
        /// </summary>
        void UpgradeDb();
    }
}
