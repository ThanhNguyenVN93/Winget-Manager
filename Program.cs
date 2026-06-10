using System;
using System.Reflection;
using System.Windows.Forms;

namespace frm_winget_upgrade
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Must be registered before any Guna type is loaded by the JIT.
            // When the EXE runs without side-by-side DLL files the CLR fires
            // AssemblyResolve and we load the DLLs from embedded resources.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException               += (s, e) => ShowFatalError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                { if (e.ExceptionObject is Exception ex) ShowFatalError(ex); };

            try { Application.Run(new Form1()); }
            catch (Exception ex) { ShowFatalError(ex); }
        }

        private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
        {
            string name         = new AssemblyName(args.Name).Name;
            string resourceName = "frm_winget_upgrade." + name + ".dll";
            using (var stream = Assembly.GetExecutingAssembly()
                                        .GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }

        private static void ShowFatalError(Exception ex) =>
            MessageBox.Show(
                "An unexpected error has occurred and the application must close.\n\n" +
                ex.Message + "\n\n" + ex.StackTrace,
                "Winget Manager — Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
    }
}
