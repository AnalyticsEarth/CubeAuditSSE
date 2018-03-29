# CubeAuditSSE

CubeAuditSSE is an Analytic Connector for Qlik Sense and QlikView. It provides the ability to audit log individual requests for data from the engine. Only Hypercubes which include the connector expression get logged and include the data items of the developer’s choice. This provides a very lightweight way to log the specific sensitive fields or keys associated with these fields when a user is viewing data at the transactional level for example.

Data that is included in the hypercube at calculation will be logged rather than specifically only the data seen by the user.

When used in a transactional table, it is recommended to use calculation conditions to ensure the data logged is limited, which if it is being logged likely means the data is sensitive and calculation conditions are best practice anyway!

## Configuration
All configuration is done in the App.config file (once compiled this file is called CubeAuditSSECore.dll.config) and the following settings can be set:

- grpcHost: The allowed remote connection IP block (default: 0.0.0.0)
- grpcPort: The port upon which the grpc communication will take place (default: 50055)
- certificateFolder: The certificate folder for secure communication with Qlik Engine
- logType: The type of logging to make (default: file) Note: file is the only method supported
- fileLogFolderWindows: The folder location for log files when running on Windows (default: c:\tmp\cubeauditsse\\)
- fileLogFolderLinux: The folder location for log files when running on Linux (default: /data/cube/)
- fileLogPattern: The pattern of logging used in file mode (default: 0) Note: reserved for future functionality

## Deployment
The connector has been written in c# using dotnet core 2.1. This makes the code portable across operating systems. By default, it can be compiled on the local machine to build a version for the current architecture.

Docker files are included to allow for build and deployment as a docker image using official Microsoft dotnet base images. A working version (as illustrated in docker-compose-deploy.yml) can be found on docker hub as analyticsearth/cubeauditsse.

## Usage
The following functions are available for use within chart expressions:

### CubeAuditSSE.LogAsSeenStrEcho([FieldtoLog])
- Input: String field value
- Output: Echo of sent value

### CubeAuditSSE.LogAsSeenStrCheck([FieldtoLog])
- Input: String field value
- Output: Single character tick

### CubeAuditSSE.LogAsSeenEcho([FieldtoLog])
- Input: Numeric field value
- Output: Echo of sent value

### CubeAuditSSE.LogAsSeenCheck([FieldtoLog])
- Input: Numeric field value
- Output: Single character tick

These presume you have called the connector CubeAuditSSE in the QMC or settings.ini file.

## File Log Format
Request will result in a folder structure with the following pattern:

/YYYYMMDD/AppID/UserID/

Within the folder a file containing a log of all requests is stored:

*YYYYMMDD* _ *AppID* _ *UserID* _ REQUEST.txt

This file contains the following fields pipe delimited:

1. Request timestamp
1. Request GUID - Random generated for each request
1. App ID - From Qlik Engine
1. User ID and Directory - From Qlik Engine

Each request will get its own file which contains the data actually logged:

*YYYYMMDD* _ *AppID* _ *UserID* _ *RequestGUID*.txt

This file contains the following fields pipe delimited:

1. Request timestamp
2. Request GUID
3. Data from Qlik Engine

Note: this logging format is quite verbose and creates a lot of files to prevent file locks. Further file format patterns are to follow. For high volume usage it is recommended to use a high concurrency data store, additional connectors for systems such as DynamoDB are to follow.
