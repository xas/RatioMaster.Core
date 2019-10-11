using System;
using System.Text;
using System.Windows.Forms;

namespace RatioMaster.Core
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        internal static void Main()
        {
            Application.EnableVisualStyles();
            EncodingProvider codePages = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(codePages);
            //// Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
