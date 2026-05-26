using System;
using System.Configuration;
using System.Windows.Forms;

using LottoDriver.Examples.CustomersApi.Common.DataAccess;

namespace LottoDriver.Examples.CustomersApi.DatabaseViewer
{
    /// <summary>
    /// Read-only viewer for the SQLite database used by the example services.
    /// Reads the database path from <c>App.config</c>'s <c>DatabasePath</c>
    /// app setting and shows the most recently scheduled draws.
    /// </summary>
    public partial class FrmMain : Form
    {
        private IDatabase _database;

        public FrmMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Reloads the data grid with the most recent draws.
        /// <para>
        /// The viewer uses a rollback at the end so it never modifies the database
        /// it inspects. This also makes it safe to run alongside the worker or
        /// Windows Service that owns the file.
        /// </para>
        /// </summary>
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                if (_database == null)
                {
                    _database = new SQLiteDatabase(ConfigurationManager.AppSettings["DatabasePath"]);
                }

                _database.BeginTransaction();
                try
                {
                    Cursor = Cursors.WaitCursor;
                    bindingSource1.SuspendBinding();

                    _database.LottoDrawFindRecent(dataTable1);
                }
                finally
                {
                    // Rollback is intentional: the viewer never writes. The
                    // transaction is opened only because the IDatabase contract
                    // requires it for any read.
                    _database.RollbackTransaction();

                    bindingSource1.ResumeBinding();
                    Cursor = Cursors.Default;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
    }
}
