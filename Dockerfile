# Taken from https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore EchoRelay.Cli
# Build and publish a release
RUN dotnet publish EchoRelay.Cli -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .

#Copy to /App and Set executable bit on entrypoint.sh script
WORKDIR /App
COPY ./entrypoint.sh ./
RUN chmod +x entrypoint.sh
ENTRYPOINT ["./entrypoint.sh"]

#Shell Entrypoint for debugging, uncomment and comment out other ENTRYPOINT to test via docker run exec -it
#ENTRYPOINT ["/bin/sh"]