using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM_AuditExport
{
    static class Exporter
    {
        #region Export Audit History to db/file
        /// <summary>
        /// Function to export the audit history to database
        /// </summary>
        public static void ExportAuditHistoryToDb()
        {
            var auditExtract = new AuditExtract();

            var counter = 0;
            Console.WriteLine();

            var totalCaseEntities = CrmUtil.GetEntityList("incident", new string[] { "incidentid" },
                new FilterExpression()
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                                {
                                    new ConditionExpression("new_legacyguid", ConditionOperator.Null)
                                }
                });


            Console.WriteLine("Starting processing at: " + DateTime.Now.ToShortTimeString() + "\n");

            // parallelize the processing of each case
            Parallel.ForEach(totalCaseEntities, entity =>
            {
                var auditDetailsList = auditExtract.GetAuditDetails(entity.Id, "incident");

                var auditExport = new AuditExport(auditDetailsList);

                // export all fields specified
                Util.AuditExportFields.ForEach(audit => auditExport.LogFieldRange(audit));

                counter++;
                Console.Write("\rCases Processed: " + counter.ToString());

            });
        }

        /// <summary>
        /// Retrieve Audit History for a list of specified case id's
        /// and write to file
        /// </summary>
        public static void ExportAuditHistoryToFile()
        {
            var counter = 0;
            var auditExtract = new AuditExtract();
            var auditDataChangeList = new List<AuditDataChange>();

            var deletedCasesGuidsList = Util.ReadDeletedCaseGuids();

            Console.WriteLine("\nTotal Deleted Cases: " + deletedCasesGuidsList.Count);

            Console.WriteLine("Starting processing at: " + DateTime.Now.ToShortTimeString() + "\n");
            foreach (var deletedCaseGuid in deletedCasesGuidsList)
            {

                auditDataChangeList = auditExtract.GetAuditDetails(new Guid(deletedCaseGuid), "incident");

                var auditExport = new AuditExport(auditDataChangeList);

                auditExport.LogDeletedCasesAudit();

                counter++;
                Console.Write("\rCases Processed: " + counter.ToString() + " / " + deletedCasesGuidsList.Count);

            }

        }
        #endregion
    }
}
