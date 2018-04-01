FROM microsoft/dotnet:2.1-sdk as builder  
 
RUN mkdir -p /root/src/app/cubeauditsse
WORKDIR /root/src/app/cubeauditsse
 
COPY CubeAuditSSECore/CubeAuditSSECore.csproj CubeAuditSSECore/CubeAuditSSECore.csproj
RUN dotnet restore CubeAuditSSECore/CubeAuditSSECore.csproj 

COPY . .
RUN dotnet publish -c release -o published 

FROM microsoft/dotnet:2.1-runtime

WORKDIR /root/  
COPY --from=builder /root/src/app/cubeauditsse/CubeAuditSSECore/published .

EXPOSE 50055/tcp
EXPOSE 19345/tcp
CMD ["dotnet","./CubeAuditSSECore.dll"]