# Multi-stage build for AiMentor.Web. Hardened in P7-T1 (non-root checks, migrations on start).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first for layer caching
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/AiMentor.Domain/AiMentor.Domain.csproj src/AiMentor.Domain/
COPY src/AiMentor.Application/AiMentor.Application.csproj src/AiMentor.Application/
COPY src/AiMentor.Infrastructure/AiMentor.Infrastructure.csproj src/AiMentor.Infrastructure/
COPY src/AiMentor.Web/AiMentor.Web.csproj src/AiMentor.Web/
RUN dotnet restore src/AiMentor.Web/AiMentor.Web.csproj

COPY src/ src/
RUN dotnet publish src/AiMentor.Web/AiMentor.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# ICU is required for ka-GE culture data; the aspnet image ships it (not the -extra/chiseled variants).
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AiMentor.Web.dll"]
