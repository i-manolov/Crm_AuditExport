using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace CRM_AuditExport
{
    public class SqlUtil
    {
        private static string _conStr;

        /// <summary>
        /// Constructor
        /// </summary>
        public SqlUtil()
        {
            // create a new Connection String when instantiating a new instance of the class
            _conStr = ConfigurationManager.ConnectionStrings["SqlDb"].ToString();
        }


        /// <summary>
        /// Insert/Update call to db by sending time range between startDate and endDate
        /// </summary>
        /// <remarks>
        /// The stored procedure loops for every day in the time range provided
        /// </remarks>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="ticketNumber"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <param name="fieldValueType"></param>
        internal void SaveIncidentFieldNameRange(DateTime startDate, DateTime endDate, string ticketNumber, string fieldName, string fieldValue, string fieldValueType)
        {
            try
            {
                using (var sqlCon = new SqlConnection(_conStr))
                {
                    using (var sqlInsertUpdateCommand = new SqlCommand("usp_InsertIncidentFieldName", sqlCon))
                    {
                        sqlInsertUpdateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                        sqlInsertUpdateCommand.Parameters.AddWithValue("@ticketnumber", ticketNumber);
                        sqlInsertUpdateCommand.Parameters.AddWithValue("@StartDate", startDate);
                        sqlInsertUpdateCommand.Parameters.AddWithValue("@EndDate", endDate);
                        sqlInsertUpdateCommand.Parameters.AddWithValue("@ColumnName", fieldName);
                        sqlInsertUpdateCommand.Parameters.AddWithValue("@ColumnValue", (fieldValue == null) ? (object)DBNull.Value : fieldValue);
                        sqlInsertUpdateCommand.Parameters.AddWithValue("@ColumnType", fieldValueType);

                        sqlCon.Open();
                        sqlInsertUpdateCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("SaveIncidentFieldNameRange", new Dictionary<string, string>() 
                    {
                        { "startDate", startDate.ToString() },
                        { "endDate", endDate.ToString() },
                        { "ticketNumber", ticketNumber }
                    }, ex);
                throw ex;
            } 
        }
    }
}
