FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /src
# Copy everything before restoring, not just the .csproj: restoring against
# a directory that's missing wwwroot/Components yet bakes an incomplete
# static-web-assets manifest into obj/, and a later --no-restore publish
# trusts that stale manifest -- blazor.web.js silently never makes it into
# wwwroot/_framework, so the interactive circuit can never connect (the app
# looks fine but every button is dead). Costs some layer-cache reuse on
# unrelated source edits; worth it for correctness on a project this size.
COPY . .
# Must target the .csproj explicitly, not just "dotnet publish": with the
# .slnx sitting right next to it, an unqualified publish auto-discovers the
# solution file and does a solution-level publish, which has the same
# missing-static-assets failure mode as above.
RUN dotnet restore Bitmagnet.WebUI.csproj
RUN dotnet publish Bitmagnet.WebUI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime

WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:4444
EXPOSE 4444

ENTRYPOINT ["dotnet", "Bitmagnet.WebUI.dll"]
