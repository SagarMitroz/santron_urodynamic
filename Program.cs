using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SantronWinApp
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            EnsureAppFolders();

            // 🔹 Load DLLs from Lib folder
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembliesFromLibFolder;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // --- ADD THIS LINE HERE ---
            // This creates your entire folder structure in "My Documents" instantly.
           // AppPathManager.CreateApplicationFolders();
            AppPathManager.InitializeApplication();
            // --------------------------

            Application.Run(new MainForm());
        }

        //Code For Auto Create 
        private static void EnsureAppFolders()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;

            //string folder1 = Path.Combine(exeFolder, "Saved Data");
            //string folder2 = Path.Combine(folder1, "ScaleAndColorSetup");

            //if (!Directory.Exists(folder1))
            //    Directory.CreateDirectory(folder1);

            //if (!Directory.Exists(folder2))
            //    Directory.CreateDirectory(folder2);
        }

        // 🔹 Assembly loader
        private static Assembly ResolveAssembliesFromLibFolder(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";

            string assemblyPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Lib",
                assemblyName);

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }
    }
}
