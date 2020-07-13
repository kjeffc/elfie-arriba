FROM arriba-base as build

WORKDIR /src/Arriba/Arriba.Server 
RUN dotnet publish --no-restore -o /app

# # #  --- Aspnet Runtime Image --- # # #
FROM mcr.microsoft.com/dotnet/core/aspnet

ENV Host--CORS=*

COPY --from=build /app /Arriba/bin/app
RUN rm /Arriba/bin/app/appsettings*.json
ADD appsettings.json /Arriba/bin/app 
WORKDIR /Arriba/bin/app
RUN ls -l

ENTRYPOINT ["dotnet", "Arriba.Server.dll"]