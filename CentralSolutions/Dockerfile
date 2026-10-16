FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src


COPY ["CentralSolutions/CentralSolutions.csproj", "CentralSolutions/"]
RUN dotnet restore "CentralSolutions/CentralSolutions.csproj"


COPY . .
WORKDIR "/src/CentralSolutions"

RUN dotnet publish "CentralSolutions.csproj" \
	-c $BUILD_CONFIGURATION \
	-o /app/publish \
	--no-restore \
	/p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN mkdir -p /app/data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CentralSolutions.dll"]
