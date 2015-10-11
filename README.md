# Crm Audit Export
A multi-threaded daily export based on Microsoft Dynamics CRM 2011 audit history changes 

### Overview
The motivation behind this tool was that out of the box, Microsoft Dynamics CRM 2011 only provides users with the ability to query the latest snapshot of the data in the system thru its application.
The Audit History functionality contains all the necessary information containing the historical values for the fields of an entity BUT users cannot query it using the applicaton.
As such,it is extremely difficult to run analytics over a time period to capture trends within the application. This console application
provides the developer to export the data from audit history and re-create the daily values for the desires fields on an entity.

Note: this tool currently queries the incident audit history logs but it can easily be updated to pull the logs of any entity

### Application specific configurations
#### App.Config
- You <b>must update</b> the following config sections: 
  - <connectionStrings>: "Crm", "Database" - enter the connection string settings and credentials
  - <appSettings>: "Fields that will be exported from the audit history" 

- The following marked config sections have defaults:
  - <appSettings>: "Directory Section" - the default directory for logging is inside the project bin folder
  - <appSettings>: "Page size for retrieved records"
  

