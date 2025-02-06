# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["HueBot/HueBot.csproj", "HueBot/"]
RUN dotnet restore "HueBot/HueBot.csproj"
COPY . .
WORKDIR "/src/HueBot"
RUN dotnet build "HueBot.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HueBot.csproj" -c Release -o /app/publish

# Final stage: use the runtime image and copy the published app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HueBot.dll"] 
