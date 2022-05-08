using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics; 

namespace Program
{
    public static class FileHandling
    {
        /// <summary>
        /// Creates the output folder and folders for the mzml converted files, the DIAUmpire Outputs, and the resulting combined files. 
        /// </summary>
        /// <param name="outputDirectoryPath"></param>
        public static void CreateOutputFolder(string outputDirectoryPath, out string convertedDataPath,
            out string dIAUmpireOutput, out string dIAUmpireTempFiles, out string combinedFiles)
        {
            convertedDataPath = Path.Combine(outputDirectoryPath, "ConvertedData");
            dIAUmpireOutput = Path.Combine(outputDirectoryPath, "DIAUmpireOutput");
            dIAUmpireTempFiles = Path.Combine(outputDirectoryPath, "DIAUmpireOutput", "TempMZXMLFiles");
            combinedFiles = Path.Combine(outputDirectoryPath, "CombinedFiles");

            Directory.CreateDirectory(outputDirectoryPath);
            Directory.CreateDirectory(convertedDataPath);
            Directory.CreateDirectory(dIAUmpireOutput);
            Directory.CreateDirectory(dIAUmpireTempFiles);
            Directory.CreateDirectory(combinedFiles);
        }
        public static string[] GetAllThermoRawFilesFromDir(string paths)
        {
            string extension = ".raw";
            return Directory.GetFiles(paths, extension);
        }
    }
}
