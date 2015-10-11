using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM_AuditExport
{
    public class AuditDataChange
    {
        public string AttributeName { get; set; }

        public string AttributeOldValue { get; set; }

        public string AttributeNewValue { get; set; }

        public string ActionType { get; set; }

       public string ModifiedBy {get; set;}

        public DateTime ModifiedDate { get; set; }

    }
}
