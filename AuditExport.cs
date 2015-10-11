using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CRM_AuditExport
{
    public class AuditExport
    {
        #region Class Properties

        private List<AuditDataChange> _listAuditDataChanges;
        private string _ticketNumber;
        private SqlUtil _sqlUtil;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>Reverses the provided list to get order from oldest -> newest</remarks>
        /// <param name="listAuditData"></param>
        /// <param name="borough"></param>
        public AuditExport(List<AuditDataChange> listAuditData)
        {
            // create a copy of the list
            _listAuditDataChanges = listAuditData;

            // reverse list order to get earliest date in beginning of list 
            _listAuditDataChanges.Reverse();

            // get ticket number
            _ticketNumber = _listAuditDataChanges.Find(x => x.AttributeName == "ticketnumber").AttributeNewValue;

            _sqlUtil = new SqlUtil();
        }

        /// <summary>
        /// Exports the audit history for the provided list of deleted cases
        /// </summary>
        public void LogDeletedCasesAudit()
        {
            Util.WriteAuditLogtoFile(_ticketNumber, _listAuditDataChanges);
        }


        /// <summary>
        /// Filter out the list to contain onyl fieldName values
        /// and call _RecursiveSaveField
        /// </summary>
        /// <param name="fieldName"></param>
        public void LogFieldRange(string fieldName)
        {
            var filteredList = _listAuditDataChanges.FindAll(x => x.AttributeName == fieldName);

            if (fieldName.Equals("statecode"))
            {
                filteredList.RemoveAll(x => x.AttributeOldValue == x.AttributeNewValue);
            }
            else
            {
                filteredList.RemoveAll(x => x.AttributeNewValue == null);
            }

            _RecursiveSaveField(filteredList, fieldName);
        }


        /// <summary>
        /// Recursively send start date, end date, fieldName, fieldValue to db
        /// to enter daily values for case
        /// </summary>
        /// <param name="filteredList"></param>
        /// <param name="fieldName"></param>
        private void _RecursiveSaveField(List<AuditDataChange> filteredList, string fieldName)
        {
            try
            {
                // list is done
                if (filteredList.Count.Equals(0))
                {
                    return;
                }
                else
                {
                    var startDate = filteredList[0].ModifiedDate;
                    var endDate = new DateTime();

                    // case has not changed value
                    if (filteredList.Count.Equals(1))
                    {
                        endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);

                        var fieldValue = filteredList[0].AttributeNewValue;

                        _sqlUtil.SaveIncidentFieldNameRange(startDate, endDate, _ticketNumber, fieldName, fieldValue, "string");

                        return;
                    }

                    // recursive call
                    else
                    {
                        var fieldValue = "";

                        endDate = filteredList[1].ModifiedDate;

                        fieldValue = filteredList[1].AttributeOldValue;

                        _sqlUtil.SaveIncidentFieldNameRange(startDate, endDate, _ticketNumber, fieldName, fieldValue, "string");

                        var tail = filteredList.Skip(1).ToList();
                        _RecursiveSaveField(tail, fieldName);
                    }

                }
            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("_RecursiveSaveField", new Dictionary<string, string>() 
                    {
                        { "ticketnumber", _ticketNumber },
                        { "filteredList", JsonConvert.SerializeObject(filteredList) }
                    }, ex);
                throw ex;
            }
        
        }
    }
}
