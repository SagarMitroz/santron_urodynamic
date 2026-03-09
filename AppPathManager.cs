//using System;
//using System.IO;

//public static class AppPathManager
//{
//    private const string AppName = "Santron Urodynamics";

//    private static string BaseUserPath =>
//    Path.Combine(
//        AppDomain.CurrentDomain.BaseDirectory,
//        "Saved Data"
//    );

//    public static void InitializeApplication()
//    {
//        string installPath = AppDomain.CurrentDomain.BaseDirectory;

//        Directory.CreateDirectory(BaseUserPath);

//        string[] folders =
//        {
//        "DoctorsData",
//        "PatientsData",
//        "ReportComment",
//        "ScaleAndColorSetup",
//        "SymtomsData",
//        "HospitalAndDocter",
//        "SystemSetup",
//        "VideoDevice",
//        "Helpdocs"
//    };

//        foreach (string folder in folders)
//        {
//            Directory.CreateDirectory(
//                Path.Combine(BaseUserPath, folder)
//            );
//        }

//        CopyFileIfNotExists(
//            Path.Combine(installPath, "DefaultFiles", "ScaleColorSetupFile.dat"),
//            Path.Combine(BaseUserPath, "ScaleAndColorSetup", "ScaleColorSetupFile.dat")
//        );

//        CopyFileIfNotExists(
//            Path.Combine(installPath, "DefaultFiles", "SystemSetupFile.dat"),
//            Path.Combine(BaseUserPath, "SystemSetup", "SystemSetupFile.dat")
//        );
//    }


//    public static string GetFolderPath(string subFolderName)
//    {
//        string path = Path.Combine(
//            BaseUserPath,
//            //"Saved Data",
//            subFolderName
//        );

//        if (!Directory.Exists(path))
//            Directory.CreateDirectory(path);

//        return path;
//    }

//    public static string GetFilePath(
//        string subFolderName,
//        string fileName,
//        string extension = ".dat")
//    {
//        string folderPath = GetFolderPath(subFolderName);

//        foreach (char c in Path.GetInvalidFileNameChars())
//            fileName = fileName.Replace(c, '_');

//        return Path.Combine(folderPath, fileName + extension);
//    }

//    private static void CopyFileIfNotExists(string source, string dest)
//    {
//        try
//        {
//            if (!File.Exists(source)) return;
//            if (File.Exists(dest)) return;

//            Directory.CreateDirectory(
//                Path.GetDirectoryName(dest)
//            );

//            File.Copy(source, dest);
//        }
//        catch
//        {
//            // prevent app crash
//        }
//    }
//}



using System;
using System.IO;

public static class AppPathManager
{
    private static string BaseDirectory =>
        AppDomain.CurrentDomain.BaseDirectory;

    // ✅ Patient data will be stored here
    private static string SamplesPath =>
        Path.Combine(BaseDirectory, "Samples");

    // ✅ Other system folders will be stored here
    private static string SystemPath =>
        Path.Combine(BaseDirectory, "System");

    public static void InitializeApplication()
    {
        string installPath = BaseDirectory;

        // Create main folders
        Directory.CreateDirectory(SamplesPath);
        Directory.CreateDirectory(SystemPath);

        // System subfolders
        string[] systemFolders =
        {
            "DoctorsData",
            "ReportComment",
            "ScaleAndColorSetup",
            "SymtomsData",
            "HospitalAndDocter",
            "SystemSetup",
            "VideoDevice",
            "Helpdocs"
        };

        foreach (string folder in systemFolders)
        {
            Directory.CreateDirectory(
                Path.Combine(SystemPath, folder)
            );
        }

        // Copy default files
        CopyFileIfNotExists(
            Path.Combine(installPath, "DefaultFiles", "ScaleColorSetupFile.dat"),
            Path.Combine(SystemPath, "ScaleAndColorSetup", "ScaleColorSetupFile.dat")
        );

        CopyFileIfNotExists(
            Path.Combine(installPath, "DefaultFiles", "SystemSetupFile.dat"),
            Path.Combine(SystemPath, "SystemSetup", "SystemSetupFile.dat")
        );
    }

    /// <summary>
    /// Returns folder path
    /// </summary>
    //public static string GetFolderPath(string subFolderName)
    //{
    //    if (subFolderName == "PatientsData")
    //    {
    //        // ✅ Patient files directly in Samples
    //        return SamplesPath;
    //    }
    //    else
    //    {
    //        // ✅ All others inside System
    //        string path = Path.Combine(SystemPath, subFolderName);

    //        if (!Directory.Exists(path))
    //            Directory.CreateDirectory(path);

    //        return path;
    //    }
    //}

    public static string GetFolderPath(string subFolderName)
    {
        // ✅ If calling "Samples" → return Samples folder directly
        if (subFolderName.Equals("Samples", StringComparison.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(SamplesPath))
                Directory.CreateDirectory(SamplesPath);

            return SamplesPath;
        }

        // ❌ Do NOT create Samples inside System accidentally
        if (subFolderName.Equals("PatientsData", StringComparison.OrdinalIgnoreCase))
        {
            return SamplesPath;
        }

        // ✅ All other folders go inside System
        string path = Path.Combine(SystemPath, subFolderName);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    /// <summary>
    /// Returns full file path
    /// </summary>
    public static string GetFilePath(
        string subFolderName,
        string fileName,
        string extension = ".dat")
    {
        string folderPath = GetFolderPath(subFolderName);

        foreach (char c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');

        return Path.Combine(folderPath, fileName + extension);
    }

    private static void CopyFileIfNotExists(string source, string dest)
    {
        try
        {
            if (!File.Exists(source)) return;
            if (File.Exists(dest)) return;

            Directory.CreateDirectory(
                Path.GetDirectoryName(dest)
            );

            File.Copy(source, dest);
        }
        catch
        {
            // prevent app crash
        }
    }
}