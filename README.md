# bitmagnet + Blazor UI

This is a fork of [bitmagnet](https://bitmagnet.io) (a self-hosted BitTorrent indexer, DHT crawler, content classifier and search engine) that replaces the original Angular web UI with a Blazor Server frontend.

The Go backend itself is untouched: same indexer, same DHT crawler, same GraphQL API and schema at `/graphql`. The new UI is a completely separate process that talks to that API the same way any other client would, so the two can be built, deployed and scaled independently.

---

## What's included

- A torrent browser backed entirely by server-side paging, sorting and search, so it stays fast against a multi-million-row library instead of loading everything into memory
- Faceted filtering by content type, source, file type and language, with counts pulled from real GraphQL aggregations rather than counted off whatever happens to be on screen
- A dashboard showing system health, worker status, queue job status and a library breakdown by content type
- A permalink page per torrent (`/torrents/permalink/{infoHash}`), including its file list
- A dark MudBlazor UI

## Setup

### Docker Compose (recommended)

Requires Docker with the Compose plugin (`docker compose`, not the standalone `docker-compose` binary).

The bundled `docker-compose.yml` is the full-featured example carried over from upstream bitmagnet: it routes the indexer's traffic through a VPN via [gluetun](https://github.com/qdm12/gluetun), and includes optional Grafana/Prometheus/Loki observability services. Before bringing it up:

1. Open `docker-compose.yml` and fill in the `gluetun` service's `VPN_SERVICE_PROVIDER` and credentials for the VPN provider in use (see the [gluetun wiki](https://github.com/qdm12/gluetun-wiki/tree/main/setup/providers) for the exact variables a given provider needs). Without this, `gluetun` will restart-loop and nothing downstream of it will start, since `bitmagnet` shares its network namespace.
2. Optionally set `TMDB_API_KEY` on the `bitmagnet` service for TMDB-backed classification.
3. From the repo root:

   ```bash
   docker compose up -d
   ```

To skip the VPN requirement entirely, see the [minimal installation example](https://bitmagnet.io/setup/installation.html) on the bitmagnet site and drop `blazor-ui` into it the same way it's wired up here: its own service, pointed at the backend's GraphQL endpoint over the compose network.

### Running the UI locally instead

`bitmagnet` shares gluetun's network namespace in this compose file, so it can't start without a working VPN connection either way. When iterating on the UI itself, the faster loop is to point a locally-run Blazor app at any already-running bitmagnet instance -- the full VPN-gated stack above, or another host on the network -- rather than restarting the whole stack on every change:

```bash
cd blazor-ui
dotnet run
```

This comes up on `http://localhost:5080` (see `Properties/launchSettings.json`) and, by default, points at `http://localhost:3333/graphql`. Change that in `appsettings.Development.json` if the bitmagnet instance in use is elsewhere.

## Access

| Service | URL | Notes |
|---|---|---|
| Web UI | `http://localhost:4444` | Torrent browser, filters, dashboard |
| GraphQL API | `http://localhost:3333/graphql` | Also serves the GraphQL Playground on `GET` |
| Postgres | `localhost:5432` | Exposed for local tooling; not needed day to day |
| Grafana | `http://localhost:3000` | Only if the observability services are running |

## Configuration

The Blazor app reads a single setting, `Bitmagnet:GraphQLEndpoint`, telling it where to send GraphQL requests. It can be set via:

- `appsettings.json` / `appsettings.Development.json` in `blazor-ui/` for local runs
- the `Bitmagnet__GraphQLEndpoint` environment variable, which is how `docker-compose.yml` sets it for the containerized build (double underscore, per ASP.NET Core's env var config convention)

Everything else -- Postgres credentials, the VPN provider, the optional TMDB key, worker selection -- is configured the same way it always was for bitmagnet; see `docker-compose.yml` and [the bitmagnet configuration docs](https://bitmagnet.io/setup/configuration.html).

## AI disclosure

The Blazor UI in this fork, and the Dockerfile fix described in its commit history, were built with AI assistance (Claude Code), directed and reviewed by the repository owner. Changes were tested against a live bitmagnet instance to verify actual behavior rather than relying on a successful compile, and decisions on architecture and scope were made by the repository owner throughout. This disclosure is included on the principle that anyone evaluating a fork should know how it was built.

## License

MIT, same as upstream bitmagnet -- see `LICENSE`.
