FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App

# Copy everything
COPY ./agentflow ./agentflow
COPY ./agentflow-server ./agentflow-server
# Restore as distinct layers
RUN dotnet restore ./agentflow-server/agentflow-server.csproj
# Build and publish a release
RUN dotnet publish --framework net9.0 ./agentflow-server/agentflow-server.csproj -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App/agentflow-server
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "agentflow-server.dll"]