FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["WebApiSample/WebApiSample.csproj", "WebApiSample/"]
RUN dotnet restore "WebApiSample/WebApiSample.csproj"
COPY . .
WORKDIR "/src/WebApiSample"
RUN dotnet build "WebApiSample.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "WebApiSample.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "WebApiSample.dll"]