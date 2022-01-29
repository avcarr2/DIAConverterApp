using System.Collections.Generic;
using System.Linq;
using IO.Mgf; 
using IO.MzML; 
using MassSpectrometry;


namespace DIAMS1Quant
{
	public static class DIAImport
	{
		public static List<MsDataScan> ImportMGF(string path)
		{
			List<MsDataScan> data = Mgf.LoadAllStaticData(path).GetAllScansList();
			return data; 
		}
		public static List<MsDataScan> ImportMZXML(string path)
		{
			List<MsDataScan> data = Mzml.LoadAllStaticData(path).GetAllScansList();
			
			return data; 
		}
		public static List<MsDataScan> SelectMS1s(List<MsDataScan> allScans)
		{
			List<MsDataScan> ms1Scans = allScans.Where(i => i.MsnOrder == 1).Select(i => i).ToList().OrderBy(i => i.RetentionTime).ToList();
			return ms1Scans; 
			
		}

	}
	public static class DIAScanModifications
	{
		public static List<MsDataScan> FixMissingMS2Fields(List<MsDataScan> allScans, string hcdEnergy, double width)
		{
			foreach (MsDataScan ms in allScans)
			{
				ms.SetIsolationWidth(width);
				ms.SetHcdEnergy(hcdEnergy); 
				ms.UpdateIsolationRange();
				UpdatePrecursorIntensity(ms);
				ms.SetFilterString(ConstructFilterString(ms));
			}
			return allScans; 
		}
		public static string ConstructFilterString(MsDataScan scan)
        {
			string min = scan.MassSpectrum.FirstX.Value.ToString();
			string max = scan.MassSpectrum.LastX.Value.ToString();  
			string detector = "FTMS";
			string pNSI = "p NSI d Full ms2";
			string isolationMz = scan.IsolationMz.ToString();
			string hcdEnergy = "hcd" + scan.HcdEnergy.ToString();
			string mzWindow = "[" + min + "-" + max + "]";

			string filterString = detector + " + " + pNSI + isolationMz + "@" + isolationMz + " " + mzWindow; 
			return filterString;
        }
		public static int UpdateOneBasedPrecursorScan(MsDataScan scan, int oneBasedPrecursorScan)
		{
			if (scan.MsnOrder == 1)
			{
				oneBasedPrecursorScan++;
			}
			else
			{
				scan.SetOneBasedPrecursorScanNumber(oneBasedPrecursorScan);
			}
			return (oneBasedPrecursorScan); 
		}
		public static void UpdatePrecursorIntensity(MsDataScan scan)
		{
			double dummyIntensity = 1.5E6; 
			if(scan.OneBasedPrecursorScanNumber >= 2)
			{
				scan.SetSelectedIonIntensity(dummyIntensity); 
			}
		}
	}
}
