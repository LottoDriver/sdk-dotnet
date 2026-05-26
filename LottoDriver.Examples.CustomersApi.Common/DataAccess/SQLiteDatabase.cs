using System;
using System.Data;
using System.Data.SQLite;

namespace LottoDriver.Examples.CustomersApi.Common.DataAccess
{
    /// <summary>
    /// SQLite-backed implementation of <see cref="IDatabase"/>.
    /// <para>
    /// Owns a single <see cref="SQLiteConnection"/>. The connection is opened on
    /// <see cref="BeginTransaction"/>, closed on commit/rollback. All other methods
    /// assume an open transaction.
    /// </para>
    /// <para>
    /// Schema migrations are versioned in the <c>config</c> table under the
    /// <c>version</c> key. <see cref="UpgradeDb"/> walks the versions in order and
    /// is safe to call on every startup. The current schema version is 4.
    /// </para>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class SQLiteDatabase : IDatabase
    {
        // DateTimeKind=Utc tells the SQLite ADO.NET provider to materialise all
        // DateTime columns as UTC rather than Unspecified, which would otherwise
        // surface as local time on round-trip.
        private const string ConnectionString = "Data Source={0};Version=3;DateTimeKind=Utc";

        // Keys used in the config table. Kept as constants because they are written
        // into raw SQL strings in the migration code.
        private static class ConfigKeys
        {
            public const string LastSeqNo = "customer_api_last_seq_no";
            public const string Version = "version";
        }

        private readonly SQLiteConnection _cn;

        private SQLiteTransaction _transaction;

        /// <summary>
        /// Constructs a database wrapper for the SQLite file at
        /// <paramref name="dbFilePath"/>. The file is created on first use by
        /// the SQLite provider when the connection is opened. No I/O happens
        /// during construction.
        /// </summary>
        public SQLiteDatabase(string dbFilePath)
        {
            _cn = new SQLiteConnection(string.Format(ConnectionString, dbFilePath));
        }

        public void BeginTransaction()
        {
            _cn.Open();
            _transaction = _cn.BeginTransaction();
        }

        public void RollbackTransaction()
        {
            if (_transaction == null) throw new Exception("Not in transaction");

            _transaction.Rollback();
            _cn.Close();
        }

        public void CommitTransaction()
        {
            if (_transaction == null) throw new Exception("Not in transaction");

            _transaction.Commit();
            _cn.Close();
        }

        public int GetLastSeqNo()
        {
            using (var cmd = CreateCommand($@"
SELECT config_value FROM config WHERE config_key = '{ConfigKeys.LastSeqNo}'
")
            )
            {
                return ExecuteScalarInt(cmd);
            }
        }

        public void SetLastSeqNo(int lastSeqNo)
        {
            using (var cmd = CreateCommand($@"
UPDATE config SET config_value = '{lastSeqNo}' WHERE config_key = '{ConfigKeys.LastSeqNo}'
")
            )
            {
                cmd.ExecuteNonQuery();
            }
        }

        public Country CountryFindByLottoDriverId(string lottoDriverId)
        {
            Country c = null;

            using (var cmd = CreateCommand(@"
SELECT id, name, lottodriver_country_id FROM country WHERE lottodriver_country_id = @lottodriver_country_id
"))
            {
                AddInParam(cmd, "lottodriver_country_id", DbType.String, lottoDriverId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        c = new Country
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            LottoDriverCountryId = reader.IsDBNull(2) ? null : reader.GetString(2)
                        };
                    }
                    reader.Close();
                }
            }

            return c;
        }

        public void CountryInsert(Country c)
        {
            using (var cmd = CreateCommand(@"
INSERT INTO country (id, name, lottodriver_country_id) VALUES (@id, @name, @lottodriver_country_id)
"))
            {
                AddInParam(cmd, "id", DbType.Int32, c.Id != 0 ? c.Id : (object) DBNull.Value);
                AddInParam(cmd, "name", DbType.String, c.Name);
                AddInParam(cmd, "lottodriver_country_id", DbType.String, c.LottoDriverCountryId ?? (object) DBNull.Value);

                cmd.ExecuteNonQuery();

                if (c.Id == 0) c.Id = (int)_cn.LastInsertRowId;
            }
        }

        public Lotto LottoFindByLottoDriverId(int lottoDriverLottoId)
        {
            Lotto l = null;

            using (var cmd = CreateCommand(@"
SELECT id, country_id, name, numbers_total, numbers_drawn, lottodriver_lotto_id 
FROM lotto 
WHERE lottodriver_lotto_id = @lottodriver_lotto_id
"))
            {
                AddInParam(cmd, "lottodriver_lotto_id", DbType.Int32, lottoDriverLottoId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        l = new Lotto
                        {
                            Id = reader.GetInt32(0),
                            CountryId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            NumbersTotal = reader.GetInt32(3),
                            NumbersDrawn = reader.GetInt32(4),
                            LottoDriverLottoId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5)
                        };
                    }
                    reader.Close();
                }

            }

            return l;
        }

        public void LottoInsert(Lotto lotto)
        {
            using (var cmd = CreateCommand(@"
INSERT INTO lotto (id, country_id, name, numbers_total, numbers_drawn, lottodriver_lotto_id)
VALUES (@id, @country_id, @name, @numbers_total, @numbers_drawn, @lottodriver_lotto_id)
"))
            {
                AddInParam(cmd, "id", DbType.Int32, lotto.Id != 0 ? lotto.Id : (object) DBNull.Value);
                AddInParam(cmd, "country_id", DbType.Int32, lotto.CountryId);
                AddInParam(cmd, "name", DbType.String, lotto.Name);
                AddInParam(cmd, "numbers_total", DbType.Int32, lotto.NumbersTotal);
                AddInParam(cmd, "numbers_drawn", DbType.Int32, lotto.NumbersDrawn);
                AddInParam(cmd, "lottodriver_lotto_id", DbType.Int32, lotto.LottoDriverLottoId ?? (object) DBNull.Value);

                cmd.ExecuteNonQuery();

                if (lotto.Id == 0) lotto.Id = (int) _cn.LastInsertRowId;
            }
        }

        public LottoDraw LottoDrawFindByLottoDriverId(long lottoDriverDrawId)
        {
            LottoDraw d = null;

            using (var cmd = CreateCommand(@"
SELECT id, lotto_id, scheduled_time_utc, draw_time_utc, recommended_closing_time_utc, status, result, extra_result, lottodriver_draw_id 
FROM lotto_draw
WHERE lottodriver_draw_id = @lottodriver_draw_id
"))
            {
                AddInParam(cmd, "lottodriver_draw_id", DbType.Int64, lottoDriverDrawId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        d = new LottoDraw
                        {
                            Id = reader.GetInt32(0),
                            LottoId = reader.GetInt32(1),
                            ScheduledTimeUtc = reader.GetDateTime(2),
                            DrawTimeUtc = reader.GetDateTime(3),
                            RecommendedClosingTimeUtc = reader.IsDBNull(4) 
                                ? reader.GetDateTime(3).AddMinutes(-1) // use draw time -1 min if null in DB
                                : reader.GetDateTime(4),
                            Status = (LottoDrawStatus)reader.GetInt32(5),
                            Result = reader.IsDBNull(6) ? null : reader.GetString(6),
                            ExtraResult = reader.IsDBNull(7) ? null : reader.GetString(7),
                            LottoDriverDrawId = reader.IsDBNull(8) ? (long?)null : reader.GetInt64(8)
                        };
                    }

                    reader.Close();
                }

            }

            return d;
        }

        public void LottoDrawInsert(LottoDraw draw)
        {
            using (var cmd = CreateCommand(@"
INSERT INTO lotto_draw (id, lotto_id, scheduled_time_utc, draw_time_utc, recommended_closing_time_utc, status, result, extra_result, lottodriver_draw_id)
VALUES (@id, @lotto_id, @scheduled_time_utc, @draw_time_utc, @recommended_closing_time_utc, @status, @result, @extra_result, @lottodriver_draw_id)
"))
            {
                AddInParam(cmd, "id", DbType.Int32, draw.Id != 0 ? draw.Id : (object) DBNull.Value);
                AddInParam(cmd, "lotto_id", DbType.Int32, draw.LottoId);
                AddInParam(cmd, "scheduled_time_utc", DbType.DateTime, draw.ScheduledTimeUtc);
                AddInParam(cmd, "draw_time_utc", DbType.DateTime, draw.DrawTimeUtc);
                AddInParam(cmd, "recommended_closing_time_utc", DbType.DateTime, draw.RecommendedClosingTimeUtc);
                AddInParam(cmd, "status", DbType.Int32, (int)draw.Status);
                AddInParam(cmd, "result", DbType.String, draw.Result ?? (object) DBNull.Value);
                AddInParam(cmd, "extra_result", DbType.String, draw.ExtraResult ?? (object) DBNull.Value);
                AddInParam(cmd, "lottodriver_draw_id", DbType.Int64, draw.LottoDriverDrawId ?? (object) DBNull.Value);

                cmd.ExecuteNonQuery();

                if (draw.Id == 0) draw.Id = (int) _cn.LastInsertRowId;
            }
        }

        public int LottoDrawUpdate(LottoDraw draw)
        {
            using (var cmd = CreateCommand(@"
UPDATE lotto_draw SET
    lotto_id = @lotto_id, scheduled_time_utc = @scheduled_time_utc, draw_time_utc = @draw_time_utc,
    recommended_closing_time_utc = @recommended_closing_time_utc,
    status = @status, result = @result, extra_result = @extra_result, lottodriver_draw_id = @lottodriver_draw_id
WHERE
    id = @id
"))
            {
                AddInParam(cmd, "lotto_id", DbType.Int32, draw.LottoId);
                AddInParam(cmd, "scheduled_time_utc", DbType.DateTime, draw.ScheduledTimeUtc);
                AddInParam(cmd, "draw_time_utc", DbType.DateTime, draw.DrawTimeUtc);
                AddInParam(cmd, "recommended_closing_time_utc", DbType.DateTime, draw.RecommendedClosingTimeUtc);
                AddInParam(cmd, "status", DbType.Int32, (int)draw.Status);
                AddInParam(cmd, "result", DbType.String, draw.Result ?? (object) DBNull.Value);
                AddInParam(cmd, "extra_result", DbType.String, draw.ExtraResult ?? (object) DBNull.Value);
                AddInParam(cmd, "lottodriver_draw_id", DbType.Int64, draw.LottoDriverDrawId ?? (object) DBNull.Value);
                AddInParam(cmd, "id", DbType.Int32, draw.Id != 0 ? draw.Id : (object) DBNull.Value);

                return cmd.ExecuteNonQuery();
            }
        }

        public void LottoDrawFindRecent(DataTable dataTable)
        {
            // Recent draws window for the WinForms viewer: from six hours ago up to
            // five minutes in the future. The +5 minutes margin shows draws that
            // are about to start.
            using (var cmd = CreateCommand(@"
SELECT d.id, d.scheduled_time_utc, d.draw_time_utc, d.recommended_closing_time_utc, l.name AS lotto_name, d.status, d.result, d.extra_result, d.lottodriver_draw_id
FROM lotto_draw d
    INNER JOIN lotto l ON (l.id = d.lotto_id)
WHERE d.scheduled_time_utc < datetime('now', '+5 minutes')
    AND d.scheduled_time_utc >= datetime('now', '-6 hours')
ORDER BY d.scheduled_time_utc DESC
"))
            {
                using (var dataAdapter = new SQLiteDataAdapter(cmd))
                {
                    dataTable.Clear();
                    dataAdapter.Fill(dataTable);
                }
            }
        }

        public void UpgradeDb()
        {
            // Walk the versions in order. Each UpgradeToVxxx step is idempotent in
            // the sense that it only runs when the stored version is below its
            // target, so calling UpgradeDb() on every startup is safe.
            var version = GetVersion();

            if (version < 1)
            {
                UpgradeToV001();
            }

            if (version < 2)
            {
                UpgradeToV002();
            }

            if (version < 3)
            {
                UpgradeToV003();
            }

            if (version < 4)
            {
                UpgradeToV004();
            }
        }

        // V004: add the extra_result column on lotto_draw, used to store the
        // JSON-serialized extra-ball groups returned by the API.
        private void UpgradeToV004()
        {
            using (var cmd = CreateCommand($@"
ALTER TABLE lotto_draw ADD extra_result NTEXT;

UPDATE config SET config_value = 4 WHERE config_key='{ConfigKeys.Version}';
"))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // V003: add the recommended_closing_time_utc column on lotto_draw.
        private void UpgradeToV003()
        {
            using (var cmd = CreateCommand($@"
ALTER TABLE lotto_draw ADD recommended_closing_time_utc DATETIME;

UPDATE config SET config_value = 3 WHERE config_key='{ConfigKeys.Version}';
"))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // V002: create the domain tables (country, lotto, lotto_draw) and their
        // indexes on the LottoDriver-side ids used for upsert lookups.
        private void UpgradeToV002()
        {
            using (var cmd = CreateCommand($@"
CREATE TABLE country (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name NTEXT NOT NULL,
    lottodriver_country_id NTEXT
);

CREATE INDEX ix_country_lottodriver_country_id ON country (lottodriver_country_id);

CREATE TABLE lotto (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    country_id INT NOT NULL,
    name NTEXT NOT NULL,
    numbers_total INT NOT NULL,
    numbers_drawn INT NOT NULL,
    lottodriver_lotto_id INT,
    CONSTRAINT fk_lotto_country_id FOREIGN KEY (country_id)
        REFERENCES country (id)
);

CREATE INDEX ix_lotto_country_id ON lotto (country_id);
CREATE INDEX ix_lotto_lottodriver_lotto_id ON lotto (lottodriver_lotto_id);

CREATE TABLE lotto_draw (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    lotto_id INT NOT NULL,
    scheduled_time_utc DATETIME NOT NULL,
    draw_time_utc DATETIME NOT NULL,
    status INT NOT NULL,
    result NTEXT,
    lottodriver_draw_id BIGINT,
    CONSTRAINT fk_lotto_draw_lotto_id FOREIGN KEY (lotto_id)
        REFERENCES lotto (id)
);

CREATE INDEX ix_lotto_draw_lotto_id ON lotto_draw (lotto_id);
CREATE INDEX ix_lotto_draw_scheduled_time_utc_status ON lotto_draw (scheduled_time_utc, status);
CREATE INDEX ix_lotto_draw_lottodriver_draw_id ON lotto_draw (lottodriver_draw_id);

UPDATE config SET config_value = 2 WHERE config_key='{ConfigKeys.Version}';
"))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // V001: bootstrap the config table that everything else depends on.
        private void UpgradeToV001()
        {
            using (var cmd = CreateCommand($@"
CREATE TABLE config (
    config_key NTEXT NOT NULL,
    config_value NTEXT NOT NULL,
    CONSTRAINT pk_config PRIMARY KEY (config_key)
);

INSERT INTO config (config_key, config_value) VALUES
    ('{ConfigKeys.Version}', '1'),
    ('{ConfigKeys.LastSeqNo}', '0')
;

"))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // Returns the current schema version, or 0 if the config table does not
        // exist yet (fresh database). The catch is deliberately broad: any error
        // reading the version is treated as "no version", which triggers the
        // initial bootstrap migration.
        private int GetVersion()
        {
            try
            {
                using (var cmd =  CreateCommand($"SELECT config_value FROM config WHERE config_key = '{ConfigKeys.Version}'"))
                {
                    return ExecuteScalarInt(cmd);
                }
            }
            catch
            {
                return 0;
            }
        }

        private SQLiteCommand CreateCommand(string commandText)
        {
            var cmd = _cn.CreateCommand();
            cmd.CommandText = commandText;
            cmd.Transaction = _transaction;

            return cmd;
        }

        private void AddInParam(SQLiteCommand cmd, string paramName, DbType paramType, object paramValue)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = paramName;
            param.Direction = ParameterDirection.Input;
            param.DbType = paramType;
            param.Value = paramValue;

            cmd.Parameters.Add(param);
        }

        private static int ExecuteScalarInt(SQLiteCommand cmd)
        {
            var obj = cmd.ExecuteScalar() ?? DBNull.Value;
            return obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
        }
    }
}
