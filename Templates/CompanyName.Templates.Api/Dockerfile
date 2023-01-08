ARG CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS builder
RUN apk update --no-cache && apk upgrade --no-cache
RUN apk add --no-cache ca-certificates wget
RUN wget -q -O /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub
RUN wget https://github.com/sgerrand/alpine-pkg-glibc/releases/download/2.35-r0/glibc-2.35-r0.apk
RUN apk add --no-cache --force-overwrite glibc-2.35-r0.apk
RUN apk fix --force-overwrite alpine-baselayout-data
RUN apk add --no-cache protoc grpc
RUN apk add --no-cache clang build-base zlib-dev
ENV PROTOBUF_PROTOC=/usr/bin/protoc GRPC_PROTOC_PLUGIN=/usr/bin/grpc_csharp_plugin DOTNET_EnableDiagnostics=0

FROM builder AS publisher
WORKDIR /app/CompanyName.ServiceName
COPY CompanyName.ServiceName.sln global.json nuget.config ./
COPY src/CompanyName.ServiceName.Api/CompanyName.ServiceName.Api.csproj src/CompanyName.ServiceName.Api/
COPY src/CompanyName.ServiceName.Shared/CompanyName.ServiceName.Shared.csproj src/CompanyName.ServiceName.Shared/
COPY src/CompanyName.ServiceName.Server/CompanyName.ServiceName.Server.csproj src/CompanyName.ServiceName.Server/
COPY src/CompanyName.ServiceName.Ef/CompanyName.ServiceName.Ef.csproj src/CompanyName.ServiceName.Ef/
COPY src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj src/CompanyName.ServiceName.Ef.Postgres/
COPY src/CompanyName.ServiceName.Client/CompanyName.ServiceName.Client.csproj src/CompanyName.ServiceName.Client/
COPY src/CompanyName.ServiceName.Protos/CompanyName.ServiceName.Protos.csproj src/CompanyName.ServiceName.Protos/
COPY src/CompanyName.ServiceName.ConsoleClient/CompanyName.ServiceName.ConsoleClient.csproj src/CompanyName.ServiceName.ConsoleClient/
COPY test/CompanyName.ServiceName.Api.Test/CompanyName.ServiceName.Api.Test.csproj test/CompanyName.ServiceName.Api.Test/
COPY test/CompanyName.ServiceName.Ef.Postgres.Test/CompanyName.ServiceName.Ef.Postgres.Test.csproj test/CompanyName.ServiceName.Ef.Postgres.Test/
RUN dotnet restore --use-current-runtime
COPY . .
ARG CONFIGURATION
RUN dotnet publish src/CompanyName.ServiceName.Api/CompanyName.ServiceName.Api.csproj \
  --use-current-runtime \
  -c $CONFIGURATION \
  -o publish \
  --no-restore \
  --self-contained

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine as runtime
ENV DOTNET_gcServer=1 DOTNET_GCHeapCount=c DOTNET_EnableDiagnostics=0
WORKDIR /app/CompanyName.ServiceName
COPY --from=publisher /app/CompanyName.ServiceName/publish .
ENTRYPOINT ["./CompanyName.ServiceName.Api"]
CMD ["--grpc", "--grpc-reflection", "--http", "--http-swagger"]
