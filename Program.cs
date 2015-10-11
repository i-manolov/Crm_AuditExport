using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;

namespace CRM_AuditExport
{
    class Program
    {
        /// <summary>
        /// Entry Point of the application
        /// </summary>
        /// <param name="args"></param>
        static void Main()
        {

            Console.WriteLine("{0}\n\t{1}\n\t{2}", "Please Choose One of the Following Actions:",
                "1.Export Daily History Data.",
                "2.Retrieve Audit Logs For Specific Cases.");

            var answer = Convert.ToInt16(Console.ReadLine());
            if (answer.Equals(1))
            {
                Exporter.ExportAuditHistoryToDb();
            }
            else if (answer.Equals(2))
            {
                Exporter.ExportAuditHistoryToFile();
            }
            else
            {
                Console.WriteLine("Undefined Option");
            }

            Console.WriteLine("\nDone!");

            Console.ReadLine();
        }
    }
}
