using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;


namespace CRM_AuditExport
{
    class AuditExtract
    {
        #region Audit Operations

        /// <summary>
        /// Returns a valid List of all audit records for a case 
        /// </summary>
        /// <remarks>
        /// Microsoft article on how to retrieve audit history data:
        /// https://msdn.microsoft.com/en-us/library/gg309735.aspx
        /// </remarks>
        /// <param name="guid"></param>
        /// <param name="entityLogicalName"></param>
        /// <param name="fallbackCreatedOn"></param>
        /// <param name="fallbackTicketNumber"></param>
        /// <param name="fallbackDueDate"></param>
        /// <param name="fallbackOwner"></param>
        /// <param name="svc"></param>
        /// <returns>List of AuditDataChange</returns>
        public List<AuditDataChange> GetAuditDetails(Guid guid, string entityLogicalName)
        {
            try
            {
                var auditDataChangeList = new List<AuditDataChange>();

                RetrieveRecordChangeHistoryResponse changeResponse;
                RetrieveRecordChangeHistoryRequest changeRequest = new RetrieveRecordChangeHistoryRequest();
                changeRequest.Target = new EntityReference(entityLogicalName, guid);

                AuditDetailCollection details;

                using (OrganizationService svc = new OrganizationService(new CrmConnection("Crm")))
                {
                    changeResponse = (RetrieveRecordChangeHistoryResponse)svc.Execute(changeRequest);
                }
                if (changeResponse != null)
                {
                    details = changeResponse.AuditDetailCollection;
                }
                else
                {
                    throw new Exception("change response was null?");
                }


                if (details != null)
                {
                    // filter thru AuditDetailCollection and build List<AuditDataChange>
                    auditDataChangeList = _ProcessAuditDetails(details);
                }

                return auditDataChangeList;
            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("GetAuditDetails", new Dictionary<string, string>() 
                    {
                        { "guid", guid.ToString() },
                        { "entityLogicalName", entityLogicalName }
                    }, ex);
                throw ex;
            }

        }
        #endregion

        #region ProcessAuditData
        /// <summary>
        /// Loops thru the audit detail collections 
        /// and builds a lift of audit data changes
        /// </summary>
        /// <param type="AuditDetailCollections" name="details"></param>
        /// <returns> Returns a List of AuditDataChanges </returns>
        private List<AuditDataChange> _ProcessAuditDetails(AuditDetailCollection details)
        {
            try
            {
                var auditExtractList = new List<AuditDataChange>();

                foreach (var detail in details.AuditDetails)
                {
                    var detailType = detail.GetType();
                    if (detailType == typeof(AttributeAuditDetail))
                    {
                        var attributeDetail = (AttributeAuditDetail)detail;
                        var attributeValue = attributeDetail.NewValue ?? attributeDetail.OldValue;
                        var modifiedDateTime = (DateTime)detail.AuditRecord.Attributes["createdon"];
                        var user = ((EntityReference)detail.AuditRecord.Attributes["userid"]).Name.ToString();

                        if (attributeValue != null && attributeValue.Attributes != null)
                        {
                            foreach (var attribute in attributeValue.Attributes)
                            {
                                if (Util.AuditExportFields.Any(item => item.Equals(attribute.Key)))
                                {

                                    var change = new AuditDataChange();
                                    Object valueNew = null;
                                    Object valueOld = null;

                                    if (attributeDetail.NewValue != null)
                                    {
                                        valueNew = (attributeDetail.NewValue.Attributes.ContainsKey(attribute.Key)) ? attributeDetail.NewValue.Attributes[attribute.Key] : null;
                                    }
                                    if (attributeDetail.OldValue != null)
                                    {
                                        valueOld = (attributeDetail.OldValue.Attributes.ContainsKey(attribute.Key)) ? attributeDetail.OldValue.Attributes[attribute.Key] : null;
                                    }

                                    change.ActionType = detail.AuditRecord.FormattedValues["action"];
                                    change.AttributeName = attribute.Key;

                                    change.AttributeNewValue = _ProcessAuditDetailBasedOnType(valueNew, attribute.Key);
                                    change.AttributeOldValue = _ProcessAuditDetailBasedOnType(valueOld, attribute.Key);

                                    change.ModifiedDate = modifiedDateTime.ToLocalTime();
                                    change.ModifiedBy = user;

                                    auditExtractList.Add(change);
                                }
                            }
                        }
                    }
                }

                return auditExtractList;
            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("_ProcessAuditDetails", new Dictionary<string, string>(), ex);
                throw ex;
            }
        }

        /// <summary>
        /// Process New/Old Value based on the fieldName type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string _ProcessAuditDetailBasedOnType(Object value, string key)
        {
            string result = null;

            switch (key)
            {
                case "statuscode":
                    result = (value != null) ? CrmUtil.LookUpDictFieldValue("statuscode", ((OptionSetValue)value).Value.ToString()) : null;
                    break;

                case "statecode":
                    result  = (value != null) ? CrmUtil.LookUpDictFieldValue("statecode", ((OptionSetValue)value).Value.ToString()) : null;
                    break;

                //get ownerid in format Guid,string -> Id, ownerType (team or systemuser)
                case "ownerid":
                    var tempVal = (EntityReference)value;
                    result = (value != null) ? tempVal.Id.ToString() + "," + tempVal.LogicalName : null;
                    break;
            }

            // handle dates
            if (Util.DateFieldList.Any(item => item.Equals(key)))
            {
                result = (value != null) ? ((DateTime)value).ToString() : null;
            }

            // handle strings
            else if (Util.StringFieldList.Any(item => item.Equals(key)))
            {
                result = (value != null) ? value.ToString() : null;
            }

            // handle dropdowns
            else if (Util.DropdownFieldList.Any(item => item.Equals(key)))
            {
                result = (value != null) ? CrmUtil.LookUpDictFieldValue(key, ((OptionSetValue)value).Value.ToString()) : null;
            }

            // handle lookup fields
            else if (Util.LookupFieldList.Any(item => item.Equals(key)))
            {
                var tempVal = (EntityReference)value;
                result = (value != null) ? tempVal.Name : null;
            }

            return result;
  
        }

        #endregion
    }
}
