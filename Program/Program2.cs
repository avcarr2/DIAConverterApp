using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using DIAMS1Quant;
using MassSpectrometry;
using IO.Mgf;
using IO.MzML; 

namespace Program
{
    public class Program2
    {
        public string PathToOutputFolder { get; set; }
        public string PathToMsConvert { get; set; }
        public string PathToDIAUmpire { get; set; }
        public string PathToDIAUmpireOutput { get; set; }
        public string PathToDIAUmpireTempFolder { get; set; }
        public string PathToOriginalData { get; set; }
        public string PathToCombinedFiles { get; set; }
        public string PathToMzMLConvertedFiles { get; set; }    
        public string PathToDIAUmpireParametersFile { get; set; }

        public void Main(string[] args)
        {
            // creates the directories in the output folder. 
            FileHandling.CreateOutputFolder(args[1], out string convertedDataPath, out string diaUmpireOutput, 
                out string diaUmpireTempFiles, out string combinedFiles);
            PathToCombinedFiles = combinedFiles; 
            PathToMzMLConvertedFiles = convertedDataPath; 
            PathToDIAUmpireOutput = diaUmpireOutput;
            PathToDIAUmpireParametersFile = args[2]; 
            //PathToDIAUmpireTempFolder = diaUmpireTempFiles; 

            // 1) convert .raw files to .mzxml and .mzml using msconvert.
            PathToMsConvert = Path.Combine(@"D:\ProgramFiles\ProteoWizard\msconvert.exe");
            PathToDIAUmpire = Path.Combine(@"D:\DIA-Umpire_v2_0");  
           
            PathToOriginalData = args[0]; 
            // get all the files with .raw extension 
            string[] fileNames = FileHandling.GetAllThermoRawFilesFromDir(PathToOriginalData);
            
            MsConvert msConv = new(PathToMsConvert);
            msConv.ConvertFiles(fileNames, PathToOriginalData, PathToOutputFolder, PathToDIAUmpireTempFolder, PathToMzMLConvertedFiles);

            // 2) Run DIAUmpire with .mzxml files using a configuration file. Output is .mgf file with postfix of _Q1, _Q2, _Q3. 
            DIAUmpire diaUmpire = new(PathToDIAUmpire, PathToDIAUmpireParametersFile);
            diaUmpire.RunDIAUmpireOnAllFiles(PathToDIAUmpireTempFolder, PathToDIAUmpireParametersFile, PathToDIAUmpireOutput);

            // 3) Combine the files. 
            CombineFiles();

            Console.WriteLine("File combination completed");    
            // Still need to test the whole thing 
        }
        /// <summary>
        /// Return value needs to be Dictionary with list because there is no guarantee or way to determine what the 
        /// length of the string[] array made from the .mgf files will be. 
        /// </summary>
        /// <param name="mzMLFile"></param>
        /// <param name="mgfDirectoryPath"></param>
        /// <param name="experimentIndicator"></param>
        /// <returns></returns>
        public Dictionary<string, List<string>> MatchMzMLAndMGFFiles(string mzmlFolderPath, string mgfDirectoryPath, 
            string experimentIndicator = "Exp")
        {
            Dictionary<string, List<string>> valuesDict = new();
            // get the mzml file names from the folder path. 
            string[] mzmlFiles = Directory.GetFiles(mzmlFolderPath, ".mzML");
            // get the mgf file names from their folder. 
            string[] mgfFiles = Directory.GetFiles(mgfDirectoryPath, ".mgf");
            // populate the dictionary based on the mzml file names and the experimentIndicator
            // regex match experimentIndicator and all characters after it until the (negative look-ahead) period.  
            Regex experimentRegex = new Regex(@""+ experimentIndicator + @".*(?=\.)"); 
            foreach(string mzmlFile in mzmlFiles)
            {
                // get the exp number using the regex. 
                var matchGroup = experimentRegex.Match(mzmlFile);
                string experimentNumber = matchGroup.Groups[1].Value;
                // experimentNumber is now the new string to match against all the mgf files. 
                List<string> matchingMgfs = mgfFiles
                    .Where(x => Regex.IsMatch(x, experimentNumber, RegexOptions.IgnoreCase) == true)
                    .ToList();
                valuesDict[mzmlFile] = matchingMgfs; 
            }
            return valuesDict; 
        }
        public void CombineFiles()
        {
            var matchedOutputs = MatchMzMLAndMGFFiles(PathToMzMLConvertedFiles, PathToDIAUmpireOutput);
            // matchedOutput is key value pair
            int fileCount = 1;
            int totalFiles = matchedOutputs.Count; 
            foreach(var matchedOutput in matchedOutputs)
            {

                for(int i = 0; i < matchedOutput.Value.Count; i++)
                {
                    // Ashley doesn't want to use Q3, so just break after the first 2. 
                    // but may need to order by the string just to make sure that it's in 
                    // some semblance of order. 
                    if (i == 2) break;
                    try
                    {
                        Program1.DoFileProcessing(matchedOutput.Key, matchedOutput.Value[i], PathToCombinedFiles); 
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message + matchedOutput.Value[i]); 
                    }
                }
                Console.WriteLine("File {0} of {1} completed.", fileCount, totalFiles); 
            }
        }
    }
    public class DIAUmpire
    {
        public string PathToDIAUmpire { get; set; }
        public string DIAParametersFilePath { get; set; }
        public List<string> FilesList { get; set; }
        public DIAUmpire(string pathToDIAUmpire, string pathToDIAUmpireParametersFile)
        {
            PathToDIAUmpire = pathToDIAUmpire;
            DIAParametersFilePath = pathToDIAUmpireParametersFile;
        }
        /// <summary>
        /// Method creates the MGF folder to hold the .mgf files from the output of DIA umpire only. 
        /// </summary>
        /// <param name="pathToDIAUmpireOutput"></param>
        public void CreateDIAUmpireMGFFolder(string pathToDIAUmpireOutput)
        {
            Directory.CreateDirectory(Path.Combine(pathToDIAUmpireOutput, "MGF"));
            Directory.CreateDirectory(Path.Combine(pathToDIAUmpireOutput, "TempFolderForProcessing"));
        }
        /// <summary>
        /// Creates the DIA process. Only the file name changes between runs, so the goal is to use this function to generate the 
        ///  process and only vary the name of the file being processed. 
        /// </summary>
        /// <param name="pathToDIAUmpire"></param>
        /// <param name="pathToParametersFile"></param>
        /// <param name="pathToOutput"></param>
        /// <returns></returns>
        public ProcessStartInfo CreateDIAProcess(string pathToDIAUmpire, string pathToParametersFile,
            string filePath)
        {
            // java -Xmx16g -jar DIA_Umpire_se.jar [path to data] [parameter file]
            ProcessStartInfo startInfo = new();
            startInfo.FileName = "java";
            startInfo.ArgumentList.Add("-jar -Xmx12G");
            startInfo.ArgumentList.Add(pathToDIAUmpire);
            startInfo.ArgumentList.Add(filePath);
            startInfo.ArgumentList.Add(pathToParametersFile);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            return startInfo;
        }
        public void RunDIAUmpireOnAllFiles(string pathToDiaTempOutput, string pathToParametersFile,
            string pathToDIAOutput)
        {
            string[] mzXMLFiles = Directory.GetFiles(pathToDiaTempOutput);
            foreach (string mzXMLFile in mzXMLFiles)
            {
                ProcessStartInfo startInfo = CreateDIAProcess(PathToDIAUmpire, pathToParametersFile,
                    mzXMLFile);
                ExecuteDIAUmpire(startInfo);
                MoveMGFFilesToMGFDirectory(pathToDiaTempOutput, pathToDIAOutput);
            }
        }
        // probably also do the rename in this step as well. 
        public void MoveMGFFilesToMGFDirectory(string pathToInitialDirectory, string pathToFinalDirectory)
        {
            string extension = ".mgf";
            string[] files = Directory.GetFiles(pathToInitialDirectory, extension);
            for (int i = 0; i < files.Length; i++)
            {
                string initial = Path.Combine(pathToInitialDirectory, files[i]);
                string final = Path.Combine(pathToFinalDirectory, files[i]);
                File.Move(initial, final);
            }
        }
        public void ExecuteDIAUmpire(ProcessStartInfo startInfo)
        {
            using (Process proc = new())
            {
                proc.StartInfo = startInfo;
                proc.Start();
            }
        }
    }
}
