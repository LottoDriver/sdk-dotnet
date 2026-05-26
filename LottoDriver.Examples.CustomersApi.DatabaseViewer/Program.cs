using System.Windows.Forms;

namespace LottoDriver.Examples.CustomersApi.DatabaseViewer
{
    /// <summary>
    /// WinForms entry point for the DatabaseViewer. Opens <see cref="FrmMain"/>
    /// which lets the user inspect the SQLite database written by the worker /
    /// Windows Service examples.
    /// </summary>
    static class Program
    {
        [System.STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
    }
}
