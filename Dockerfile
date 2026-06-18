FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PsikologProje_Void.csproj", "./"]
RUN dotnet restore "PsikologProje_Void.csproj"

COPY . .
RUN dotnet publish "PsikologProje_Void.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "PsikologProje_Void.dll"]
