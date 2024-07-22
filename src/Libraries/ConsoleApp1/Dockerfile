# Use the official runtime image
#FROM mcr.microsoft.com/dotnet/sdk:8.0
#FROM alpine:latest
#
## Copy the published files from the host to the container
#COPY ./publish /app
#RUN chmod +x /app/entrypoint.sh
#WORKDIR /app
#
#ENTRYPOINT ["./entrypoint.sh"]
#
#

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine as build

RUN apk add --no-cache bash
RUN apk add --no-cache ca-certificates
RUN apk add --no-cache libstdc++ libgcc

COPY ./publish /app

RUN dotnet tool install --global dotnet-trace
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /app

ENTRYPOINT ["./entrypoint.sh"]
