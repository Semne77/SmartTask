# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY *.sln .
COPY SmartTask.Client/*.csproj SmartTask.Client/
COPY SmartTask.Server/*.csproj SmartTask.Server/
RUN dotnet restore
COPY . .
RUN dotnet publish SmartTask.Server -c Release -o /app/publish

# ✅ Copy the file after publish so it ends up in final image
COPY SmartTask.Server/prompt.txt /app/publish/

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartTask.Server.dll"]