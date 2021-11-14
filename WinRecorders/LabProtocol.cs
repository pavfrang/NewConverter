
using Microsoft.Office.Interop.Excel;
using Paulus.Excel;

namespace WinRecorders
{
    public class LabProtocol
    {
        public string ProtocolPath { get; set; }
        public string SummaryWorksheetName { get; set; }

        public int StartRow { get; set; }


        public int LastRow { get; set; }


        #region Excel related properties
        public Worksheet SummaryWorksheet { get; set; }
        public Range FirstHeaderCell { get; set; }
        #endregion


        ExcelInfo info; Workbook wb;
        Microsoft.Office.Interop.Excel.Application excel;

        public void Load()
        {
            excel = ExcelExtensions.OpenExcel(ProtocolPath, out info, out wb);

            //LoadWorksheetsToComboBox(wb);
        }

        public void Quit()
        {
            if (info.IsNewInstance) excel.QuitOrRestoreSettings(ref info);
        }
    }
}
