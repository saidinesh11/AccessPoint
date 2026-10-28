FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY App.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release --no-restore -o /out
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/data && chown $APP_UID /app/data
ENV ConnectionStrings__App="Data Source=/app/data/application.db"
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "App.dll"]
