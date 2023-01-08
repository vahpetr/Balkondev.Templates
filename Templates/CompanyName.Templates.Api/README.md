# CompanyName.ServiceName

GRPC API service ServiceName of company CompanyName.

## How create project from template

```sh
# instal templates
dotnet new install .

# create project
dotnet new balkondev -n MyCompany.MyService -o MyCompany.MyService --CompanyName MyCompany --ServiceName MyService --ApiName my-service --ResourceName my-items --EntityName MyItem --LocaleName en

# go to project
cd MyCompany.MyService

# next step see below in 'How run'

# uninstall templates
# dotnet new uninstall .
```

## How run

```sh
# install dotnet ef
dotnet tool install --global dotnet-ef
# update dotnet ef
dotnet tool update --global dotnet-ef

# create postgres migration
dotnet ef migrations add ServiceNameDbContext_Initial \
  -p src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj \
  -c CompanyName.ServiceName.Ef.Postgres.Data.ServiceName.ServiceNameDbContext \
  -o Migrations/ServiceName

# create sql script
# if you want get pure sql
dotnet ef migrations script \
  -p src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj \
  -c CompanyName.ServiceName.Ef.Postgres.Data.ServiceName.ServiceNameDbContext \
  -o src/CompanyName.ServiceName.Ef.Postgres/Migrations/ServiceName/ServiceName.sql

# run db
docker compose up db -d

# run project
dotnet run --project src/CompanyName.ServiceName.Api/CompanyName.ServiceName.Api.csproj --migrate --seed --grpc --grpc-reflection --http --http-swagger

# run console client
dotnet run --project src/CompanyName.ServiceName.ConsoleClient/CompanyName.ServiceName.ConsoleClient.csproj
```

## Development command

```sh
# build docker release local platform
docker build -t companynamelower/servicenamelower_api . --progress=plain

# build docker debug
docker build -t companynamelower/servicenamelower_api . --progress=plain --build-arg CONFIGURATION=Debug

# build docker release strict platform
docker build -t companynamelower/servicenamelower_api . --progress=plain --platform linux/amd64

# enter into container
docker run -it --rm --name companynamelower_servicenamelower_api --entrypoint sh companynamelower/servicenamelower_api

# build docker compose
docker compose build --no-cache

# run docker compose db+api
docker compose up

# force stop db
# docker compose rm db -v -f

# see all volumes
# docker volume

# remove volume
# replace companynametemplatesapi to folder name
# docker volume rm companynametemplatesapi_companynamelower_servicenamelower_db_data
```

## Work with migrations

```sh
# example. create new postgres migration
dotnet ef migrations add ServiceNameDbContext_MigrationName \
  -p src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj \
  -c CompanyName.ServiceName.Ef.Postgres.Data.ServiceName.ServiceNameDbContext \
  -o Migrations/ServiceName

# example. recreate sql script
dotnet ef migrations script \
  -p src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj \
  -c CompanyName.ServiceName.Ef.Postgres.Data.ServiceName.ServiceNameDbContext \
  -o src/CompanyName.ServiceName.Ef.Postgres/Migrations/ServiceName/ServiceName.sql

# remove migrations
dotnet ef migrations remove --force --project src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj && rm src/CompanyName.ServiceName.Ef.Postgres/Migrations/ServiceName/ServiceName.sql

# update database
dotnet ef database update --project src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj

# drop database
dotnet ef database drop --project src/CompanyName.ServiceName.Ef.Postgres/CompanyName.ServiceName.Ef.Postgres.csproj
```

## API check

```sh
# check http
# install for mac "brew install curl"
curl http://localhost:5000/

# check http2 (insecure)
# install for mac "brew install grpcurl"
grpcurl -plaintext localhost:5001 describe
```

## Create jwt token

```sh
dotnet user-jwts create --scope "servicenamelower:full" --scope "servicenamelower:read" --role "admin" --role "manager" --claim "permission=action1 action2" --project "src/CompanyName.ServiceName.Api/CompanyName.ServiceName.Api.csproj"
```

## TODO

1. Translate to EN
2. Add DI and ServiceErrors checks to console client
3. Add support VersionName
4. Add Auth example
5. Add health check https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-7.0
6. Add OpenTelemetry https://opentelemetry.io/docs/instrumentation/net/
7. Add support NATS
8. Add support Redis
