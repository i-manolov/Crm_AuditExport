using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using System.Configuration;

namespace CRM_AuditExport
{
    public class CrmUtil
    {
        #region Class Variables
        private static Dictionary<Guid, string> _BoroughDict { get; set; }
        private static Dictionary<string, string> _StatusCodeDict { get; set; }

        private static Dictionary<Guid, string> _UserBusinessUnitDict { get; set; }
        private static Dictionary<Guid, string> _TeamBusinessUnitDict { get; set; }

        private static Dictionary<string, string> _CompletionTimeFrameDict { get; set; }
        private static Dictionary<string, string> _BridgeDirectionsDict { get; set; }

        private static readonly Dictionary<int, string> _StateCodeDict = new Dictionary<int, string>() 
        { 
            { 0, "active" },
            { 1, "resolved" },
            { 2, "canceled"}
        };

        private const string _teamFieldName = "name";
        private const string _newNameField = "new_name";
        #endregion

        static CrmUtil()
        {
            // All Dicts are populated with key = id id and val = text desc
            _StatusCodeDict = _ProcessMetadataDict(_GetPicklistMetaData("incident", "statuscode"));

            _CompletionTimeFrameDict = _ProcessMetadataDict(_GetPicklistMetaData("incident", "new_completiontimeframe"));

            _BridgeDirectionsDict = _ProcessMetadataDict(_GetPicklistMetaData("incident", "new_bridgedirection"));

            _BoroughDict = _ProcessToDict(GetEntityList("new_borough", new string[] { _newNameField }), _newNameField, false);

            // get All Users and Their Business Units
            _UserBusinessUnitDict = _ProcessToDict(GetEntityList("systemuser", new string[] { "businessunitid" }), "businessunitid", true);
            _TeamBusinessUnitDict = _ProcessToDict(GetEntityList("team", new string[] { "businessunitid" }), "businessunitid", true);
        }

        #region General

        /// <summary>
        /// Get Any Entity
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="columnArray"></param>
        /// <param name="fe"></param>
        /// <returns>List<Entity></returns>
        public static List<Entity> GetEntityList(string entityName, string[] columnArray, FilterExpression fe = null)
        {
            QueryExpression getEntitiesQuery = _GenerateQueryExpression(entityName, columnArray, fe);

            var result = _RetrieveEntities(getEntitiesQuery);
            return result;
        }

        /// <summary>
        /// Generate Query Expression based on the entityName, columns to be returned,
        /// and filter expression provided
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="columnArray"></param>
        /// <param name="fe"></param>
        /// <returns></returns>
        private static QueryExpression _GenerateQueryExpression(string entityName, string[] columnArray = null,
            FilterExpression fe = null)
        {
            ColumnSet columnSet;

            // if columnArray is null get all properties for the entity
            if (columnArray == null)
            {
                columnSet = new ColumnSet();
            }
            else
            {
                columnSet = new ColumnSet(columnArray);
            }
            QueryExpression getEntitiesQuery = new QueryExpression()
            {
                EntityName = entityName,
                ColumnSet = columnSet,
                PageInfo =
                    {
                        Count = Convert.ToInt16(ConfigurationManager.AppSettings["PageSize"]),
                        PageNumber = 1,
                        PagingCookie = null
                    }

            };

            if (fe != null)
            {
                getEntitiesQuery.Criteria = fe;
            }

            return getEntitiesQuery;
        }

        /// <summary>
        /// Retrieve all entities based on the query expression provided
        /// </summary>
        /// <param name="qe"></param>
        /// <returns></returns>
        private static List<Entity> _RetrieveEntities(QueryExpression qe)
        {
            var entitiesList = new List<Entity>();
            try
            {
                using (OrganizationService svc = new OrganizationService(new CrmConnection("Crm")))
                {
                    while (true)
                    {
                        EntityCollection caseEntitiesPerPage = svc.RetrieveMultiple(qe);

                        if (caseEntitiesPerPage != null)
                        {
                            entitiesList.AddRange(caseEntitiesPerPage.Entities);
                        }
                        if (caseEntitiesPerPage.MoreRecords)
                        {
                            qe.PageInfo.PageNumber++;
                            qe.PageInfo.PagingCookie = caseEntitiesPerPage.PagingCookie;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine("Total " + qe.EntityName + "s: " + entitiesList.Count());

                return entitiesList;
            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("_RetrieveEntities", new Dictionary<string, string>() 
                { 
                    { "entityName", qe.EntityName}
                }, ex);
                throw ex;
            }
        }
        #endregion

        #region Status and State Codes operations
        /// <summary>
        /// Get PickList Metadata
        /// </summary>
        /// <param name="entityLogicalName"></param>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        private static OptionMetadataCollection _GetPicklistMetaData(string entityLogicalName, string logicalName)
        {
            var result = new OptionMetadataCollection();

            RetrieveAttributeRequest request = new RetrieveAttributeRequest();
            request.EntityLogicalName = entityLogicalName;
            request.LogicalName = logicalName; // get the reference type
            request.RetrieveAsIfPublished = true;

            RetrieveAttributeResponse response;

            try
            {
                using (OrganizationService svc = new OrganizationService(new CrmConnection("Crm")))
                {
                    response = (RetrieveAttributeResponse)svc.Execute(request);
                }

                if (response.AttributeMetadata is StatusAttributeMetadata)
                {
                    StatusAttributeMetadata picklist = (StatusAttributeMetadata)response.AttributeMetadata;
                    OptionMetadataCollection omd = picklist.OptionSet.Options;
                    result = omd;
                }
                else
                {
                    PicklistAttributeMetadata picklist = (PicklistAttributeMetadata)response.AttributeMetadata;
                    OptionMetadataCollection omd = picklist.OptionSet.Options;
                    result = omd;
                }
                return result;

            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("_GetPicklistMetaData", new Dictionary<string, string>() 
                    {
                        { "entityLogicalName", entityLogicalName },
                        { "logicalName", logicalName}
                    }, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Create a dictionary with all option metadata with key = optionalSetValue, val = text label
        /// </summary>
        /// <param name="omd"></param>
        /// <returns></returns>
        private static Dictionary<string, string> _ProcessMetadataDict(OptionMetadataCollection omd)
        {
            try
            {
                var resultDict = new Dictionary<string, string>();

                foreach (var option in omd)
                {
                    string label = option.Label.UserLocalizedLabel.Label;
                    string value = option.Value.ToString();
                    resultDict.Add(value, label);
                }
                return resultDict;
            }
            catch (Exception ex)
            {
                Util.WriteErrorToLog("_ProcessMetadataDict", new Dictionary<string, string>(), ex);
                throw ex;
            }
        }
        #endregion

        #region misc
        /// <summary>
        /// Look up private dictionary values based on fieldname and key
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string LookUpDictFieldValue(string fieldName, string key)
        {

            try
            {
                switch (fieldName)
                {
                    case "ownerid":
                        return _GetBusinessUnitName(key);

                    case "statuscode":
                        return _StatusCodeDict[key];

                    case ("new_completiontimeframe"):
                        return _CompletionTimeFrameDict[key];

                    case ("new_bridgedirection"):
                        return _BridgeDirectionsDict[key];

                    case ("statecode"):
                        return _StateCodeDict[Convert.ToInt32(key)];

                    default:
                        Util.WriteErrorToLog("LookUpDictFieldValue", new Dictionary<string, string>() 
                            {
                                { "field", fieldName },
                                { "key", key }
                            }, new Exception("No implementation"));
                        return null;
                }
            }
            catch (KeyNotFoundException ex)
            {
                if (key.Equals("4bdf659f-5c06-e211-b351-005056997b22,team"))
                {
                    return "Traffic Management";
                }
                else
                {
                    Util.WriteErrorToLog("LookUpDictFieldValue", new Dictionary<string, string>() 
                        {
                            { "field", fieldName },
                            { "key", key }
                        }, ex);
                    return null;
                }

            }

        }

        /// <summary>
        /// Look up Business Unit Name for system user or a team
        /// </summary>
        /// <param name="incidentOwnerGuidAndType"></param>
        /// <returns></returns>
        private static string _GetBusinessUnitName(string incidentOwnerGuidAndType)
        {
            var tupleOwnerGuidAndType = _ProcessGuidAndOwnerType(incidentOwnerGuidAndType);

            var ownerGuid = tupleOwnerGuidAndType.Item1;
            var ownerType = tupleOwnerGuidAndType.Item2;
            if (ownerType.Equals("systemuser"))
            {
                return _UserBusinessUnitDict[ownerGuid];
            }
            else if (ownerType.Equals("team"))
            {
                return _TeamBusinessUnitDict[ownerGuid];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Return tuple of owner guid and team/system user type
        /// </summary>
        /// <param name="incidentOwnerGuidAndType"></param>
        /// <returns></returns>
        private static Tuple<Guid, string> _ProcessGuidAndOwnerType(string incidentOwnerGuidAndType)
        {
            var commaSepList = incidentOwnerGuidAndType.Split(',');

            if (commaSepList.Count() != 2)
            {
                throw new Exception("Comma separated list for: incidentGuidAndOwnerType = " + incidentOwnerGuidAndType + " Should Return two values");
            }

            return new Tuple<Guid, string>(new Guid(commaSepList[0]), commaSepList[1]);
        }

        /// <summary>
        /// Create a dictionary from a list of entities 
        /// </summary>
        /// <param name="entityList"></param>
        /// <param name="fieldName"></param>
        /// <param name="isRef"></param>
        /// <returns>A dictionary of key=entity.Id and vall = specified val for the fieldName</returns>
        private static Dictionary<Guid, string> _ProcessToDict(List<Entity> entityList, string fieldName, bool isRef)
        {

            if (isRef.Equals(true))
            {
                return entityList.Distinct()
                    .ToDictionary(item => item.Id, item => ((EntityReference)item[fieldName]).Name);
            }
            else
            {
                return entityList.Distinct().ToDictionary(item => item.Id, item => item[fieldName].ToString());
            }

        }
        #endregion
    }
}
