# syntax=docker/dockerfile:1
# Taken from https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build
LABEL org.opencontainers.image.authors="github:EchoTools"

ARG BUILD_CONFIG="Release"

COPY . /source

WORKDIR /source

# Leverage cache
RUN --mount=type=cache,id=nuget,target=./nuget-packages \
    dotnet publish EchoRelay.Cli \
    --configuration ${BUILD_CONFIG} \
    --use-current-runtime --self-contained false -o /app

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS final
WORKDIR /app

COPY --from=build /app .

# Run the app using an non-privileged user
ARG UID=10001
RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/tmp" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "${UID}" \
    appuser

RUN mkdir /data  && chown 10001 /data

USER appuser

WORKDIR /app

VOLUME /data

ENTRYPOINT ["dotnet", "EchoRelay.Cli.dll"]
