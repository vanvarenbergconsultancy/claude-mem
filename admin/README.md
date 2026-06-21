# ClaudeMem Admin

Admin sidecar for the claude-mem server-beta. Provides a secured API and Blazor UI for managing teams, projects, API keys, observations, and jobs.

## Prerequisites

- .NET 10 SDK
- Docker (required for integration tests — see setup below)

## Running the API

```bash
dotnet run --project src/ClaudeMem.Admin.Api
```

The API requires an admin key, either mounted as a Docker secret at `/run/secrets/admin_api_key` or set via the `AdminApiKey` configuration key.

## Running tests

```bash
dotnet test
```

Integration tests spin up a real PostgreSQL instance via [Testcontainers](https://dotnet.testcontainers.org/). Docker must be reachable from the machine running the tests.

### Docker setup for Windows developers (without Docker Desktop)

Docker Desktop is **not required**. If you run Docker inside WSL2, you can expose the Docker daemon over TCP so that tests invoked from Windows (e.g. Visual Studio) can reach it.

**One-time setup per machine:**

**1. Configure the Docker daemon in WSL2 to listen on TCP**

Edit (or create) `/etc/docker/daemon.json` inside your WSL2 distro:

```json
{
  "hosts": ["unix:///var/run/docker.sock", "tcp://127.0.0.1:2375"]
}
```

> **Security note:** `127.0.0.1:2375` is loopback-only inside WSL2. It is accessible from Windows via WSL2's automatic port forwarding, but not from other machines on your network. No TLS is required for this local-only setup.

**2. Restart the Docker daemon**

With systemd (Ubuntu 22.04+):

```bash
sudo systemctl restart docker
```

Without systemd (older distros or manual start):

```bash
sudo service docker restart
```

**3. Verify Docker is reachable from Windows**

In a Windows terminal (PowerShell or CMD):

```powershell
curl http://127.0.0.1:2375/version
```

You should receive a JSON response with the Docker Engine version.

**4. Set `DOCKER_HOST` on Windows**

Add a system or user environment variable so that Testcontainers and any Docker tooling on Windows know where to find the daemon:

```
DOCKER_HOST=tcp://127.0.0.1:2375
```

In PowerShell (user-level, persists across sessions):

```powershell
[Environment]::SetEnvironmentVariable("DOCKER_HOST", "tcp://127.0.0.1:2375", "User")
```

Restart Visual Studio after setting the variable so it picks it up.

**If you use Docker Desktop for Windows** you do not need any of the above — Docker Desktop exposes the daemon to Windows automatically and Testcontainers finds it via the named pipe.

**If you use WSL2 without Docker Desktop for Windows** you should set the tcp address for the ```DOCKER_HOST``` env var to ```127.0.0.1``` and never to ```localhost```.
Windows resolves localhost to IPv6 first, causing a 1-3s timeout per Docker API call before falling back to IPv4.

### Switching the PostgreSQL provider

The test infrastructure supports multiple database providers controlled by configuration. Priority order (highest wins):

| Source                | Key                                                  |
| --------------------- | ---------------------------------------------------- |
| Code (programmatic)   | `PostgresProviderFactory.Create(forceProvider: ...)` |
| appsettings.Test.json | `"PostgresProvider": "testcontainers"`               |
| Environment variable  | `TEST_POSTGRES_PROVIDER=testcontainers`              |
| Default               | `testcontainers`                                     |

Currently only `testcontainers` is implemented. A `Vvc.Testing.PostgreSQL` package providing an embedded (no-Docker) option is planned — see [`docs/plans/vvc-testing-postgresql.md`](../vvc.common/docs/plans/vvc-testing-postgresql.md) in vvc.common.
