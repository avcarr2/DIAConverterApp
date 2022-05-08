using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO; 

namespace Program
{
    public class MsConvert
    {
        public string PathToMsConvert { get; set; }
        public string PathToOutputDirectory { get; set; }
        public string BinaryEncodingPrecisiong { get; set; }
        public string ZlibCompression { get; set; }
        public string TitleMakerFilter { get; set; }
        public Dictionary<string, string> MsConvertParameters { get; set; }
        public MsConvert(string path)
        {
            PathToMsConvert = path;
        }
        public MsConvert()
        {

        }

        // MsConvert needs: 
        /*
         * 1) output directory (-o)
         * 2) text files of a list of the filenames to convert. 
         * 3) filters 
         *      1. Peak Picking: peakPicking vendor msLevel=1-
         *      2. titleMaker: <RunId>.<ScanNumber>.<ScanNumber>.<ChargeState> File:"<SourcePath>".NativeID:"<Id>"
         * 4) command: set extension -e mzXML or mzML
         * 5) command: to convert --mzML or --mzXML 
         * 6) Binary encoding precision: 64-bit. --64
         * 7) Write index (doesn't need command) 
         * 8) zlib compression -z
         * 9) TPP compatibility (Idk what command this is) 
         */


        /// <summary>
        /// Uses MsConvert to convert all .raw files to mzml files. Output directory
        /// should be a the ConvertedData folder in output. 
        /// </summary>
        /// <param name="pathToData"></param>
        public ProcessStartInfo ConvertRawToMzML(string fileNamesFilePath, string pathToOutputDirectory)
        {
            ProcessStartInfo startInfo = new();
            startInfo.FileName = PathToMsConvert;
            startInfo.ArgumentList.Add("msconvert");
            startInfo.ArgumentList.Add("-f " + fileNamesFilePath);
            startInfo.ArgumentList.Add("--mzML");
            startInfo.ArgumentList.Add("-o " + PathToOutputDirectory);
            startInfo.ArgumentList.Add("--64");
            startInfo.ArgumentList.Add("--zlib" + " " + "--filter peakPicking true [1,2]");
            return startInfo;
        }
        /// <summary>
        /// Converts to mzXML format for DIAUmpire processing. Puts folders in a temporary folder under DIAUmpire
        /// </summary>
        /// <param name="fileNamesFilePath"></param>
        public ProcessStartInfo ConvertRawToMzXml(string fileNamesFilePath, string outputPath)
        {
            ProcessStartInfo startInfo = new();
            startInfo.FileName = PathToMsConvert;
            startInfo.ArgumentList.Add("msconvert");
            startInfo.ArgumentList.Add("-f " + fileNamesFilePath);
            startInfo.ArgumentList.Add("--mzXML");
            startInfo.ArgumentList.Add("-o " + PathToOutputDirectory);
            startInfo.ArgumentList.Add("--64");
            startInfo.ArgumentList.Add("--zlib" + " " + "--filter peakPicking true [1,2]");
            return startInfo;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="files"></param>
        /// <param name="pathToData"></param><summary> original data. not sure if I need this. </summary>
        /// <param name="pathToOutput"></param> <summary>Path to the "convertedData" folder in the output folder. </summary>
        public void ConvertFiles(string[] files, string pathToData, string pathToOutput,
            string pathToDIAUmpireTempOutput, string pathToConvertedDataOutput)
        {
            // Create text file with filenames
            string fileNamespath = WriteFileNamesTextFile(files, pathToOutput);
            var mzMLConversion = ConvertRawToMzML(fileNamespath, pathToConvertedDataOutput);
            var mzXMLConversion = ConvertRawToMzXml(fileNamespath, pathToDIAUmpireTempOutput);
            ExecuteMsConvertProcess(mzMLConversion);
            ExecuteMsConvertProcess(mzXMLConversion);
        }
        public void ExecuteMsConvertProcess(ProcessStartInfo startInfo)
        {
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process proc = new Process())
            {
                proc.StartInfo = startInfo;
                proc.Start();
            }
        }
        public string WriteFileNamesTextFile(string[] filenames, string path)
        {
            string filenamesFilePath = Path.Combine(path, "FilnamesFile.txt");
            using (StreamWriter filenamesFile = new StreamWriter(filenamesFilePath))
            {
                foreach (string filename in filenames)
                {
                    filenamesFile.WriteLine(filename);
                }
            }
            return filenamesFilePath;
        }
    }
}
