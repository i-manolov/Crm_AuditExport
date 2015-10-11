using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.IO;
using System.Text;

namespace CRM_AuditExport
{
    public static class Util
    {
        #region Properties

        // private Variable Lists of case fields by data type
        public static List<string> StringFieldList = ConfigurationManager.AppSettings["StringFieldList"].ToString().Split(',').ToList();

        public static List<string> DateFieldList = ConfigurationManager.AppSettings["DateFieldList"].ToString().Split(',').ToList();

        public static List<string> DropdownFieldList = ConfigurationManager.AppSettings["DropdownFieldList"].ToString().Split(',').ToList();

        public static List<string> LookupFieldList = ConfigurationManager.AppSettings["LookupFieldList"].ToString().Split(',').ToList();

        public static List<string> RefFieldList = ConfigurationManager.AppSettings["RefFieldList"].ToString().Split(',').ToList();

        // a concatanated list of all case fields 
        public static List<string> AuditExportFields
        {
            get
            {
                return RefFieldList.Concat(StringFieldList).Concat(DateFieldList).Concat(DropdownFieldList)
                    .Concat(LookupFieldList).ToList();
            }
        }
        #endregion

        private static void _WriteLog(string message)
        {
            try
            {
                string dir = @"" + ConfigurationManager.AppSettings["ErrorLogDir"].ToString();
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                string filePath = dir + "err_log" + ".txt";

                using (StreamWriter write = new StreamWriter(filePath))
                {
                    write.Write("AS OF: " + DateTime.Now.ToString() + Environment.NewLine + Environment.NewLine + message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static List<string> ReadDeletedCaseGuids()
        {
            try
            {
                var deletedCasesGuidsPath = ConfigurationManager.AppSettings["DeletedCasesGuidsPath"];

                var fileContents = File.ReadAllText(deletedCasesGuidsPath);

                var modifiedFileContents = fileContents.Replace("\r", string.Empty);

                var contentsArray = modifiedFileContents.Split(Environment.NewLine.ToCharArray()).ToList();

                return contentsArray;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void WriteAuditLogtoFile(string fileName, List<AuditDataChange> auditDataChangesList)
        {
            try
            {
                var logAuditHistoryPath = ConfigurationManager.AppSettings["LogAuditHistoryPath"];

                if (!System.IO.Directory.Exists(logAuditHistoryPath))
                {
                    System.IO.Directory.CreateDirectory(logAuditHistoryPath);
                }

                var filePath = logAuditHistoryPath + fileName + ".csv";
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("sep=|");
                    writer.Write("AttributeName|OldValue|NewValue|ActionType|ModifiedDate|ModifiedBy\n");

                    foreach (var auditDataChange in auditDataChangesList)
                    {
                        var stringToWrite = auditDataChange.AttributeName + "|" + auditDataChange.AttributeOldValue + "|" +
                            auditDataChange.AttributeNewValue + "|" + auditDataChange.ActionType + "|" + auditDataChange.ModifiedDate 
                            + "|" + auditDataChange.ModifiedBy + "\n";
                        writer.Write(stringToWrite);
                    }

                }
            }
            catch (Exception ex) 
            {
                throw new Exception("In WriteAuditLog: " + ex.Message);
            }
        }

        public static void WriteErrorToLog(string functionName, Dictionary<string,string> paramDict, Exception ex = null)
        { 
            StringBuilder errorMessage = new StringBuilder();
            errorMessage.Append("In " + functionName + " with params: ");
            paramDict.ToList().ForEach(x => errorMessage.Append(x.Key + " = " + x.Value));
            if (ex != null)
            {
                errorMessage.Append(". With Message: " + ex.Message + " . Inner Exception: " + ex.InnerException);
            }
            _WriteLog(errorMessage.ToString());

        }
    }
}
