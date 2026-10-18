# Izgradnja i pokretanje API-ja (kontekst: folder Server).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AITutor.Core/AITutor.Core.csproj AITutor.Core/
COPY AITutor_api/AITutor_api.csproj AITutor_api/
RUN dotnet restore AITutor_api/AITutor_api.csproj
COPY AITutor.Core/ AITutor.Core/
COPY AITutor_api/ AITutor_api/
RUN dotnet publish AITutor_api/AITutor_api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "AITutor_api.dll"]
