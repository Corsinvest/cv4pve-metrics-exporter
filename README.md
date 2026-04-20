# cv4pve-metrics-exporter

```
   ______                _                      __
  / ____/___  __________(_)___ _   _____  _____/ /_
 / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
/ /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
\____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Metrics Exporter for Proxmox VE (Made in Italy)
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-metrics-exporter.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-metrics-exporter.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-metrics-exporter/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-metrics-exporter/releases)
[![NuGet](https://img.shields.io/nuget/v/Corsinvest.ProxmoxVE.Metrics.Exporter.Api.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Corsinvest.ProxmoxVE.Metrics.Exporter.Api/)
[![WinGet](https://img.shields.io/winget/v/Corsinvest.cv4pve.metrics-exporter?style=flat-square&logo=windows)](https://winstall.app/apps/Corsinvest.cv4pve.metrics-exporter)
[![AUR](https://img.shields.io/aur/version/cv4pve-metrics-exporter?style=flat-square&logo=archlinux)](https://aur.archlinux.org/packages/cv4pve-metrics-exporter)

> Exports Proxmox VE cluster metrics to Prometheus — nodes, VMs, containers, storage, HA, replication, subscription, SMART.

---

## Quick Start

```bash
wget https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/download/VERSION/cv4pve-metrics-exporter-linux-x64.zip
unzip cv4pve-metrics-exporter-linux-x64.zip
./cv4pve-metrics-exporter --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD run
```

With API token (recommended):

```bash
./cv4pve-metrics-exporter --host=YOUR_HOST --api-token=user@realm!token=uuid run
```

Scrape endpoint: `http://localhost:9221/metrics/`

---

## Profiles

| Profile | Use case | API calls per scrape |
|---------|----------|---------------------|
| **Fast** | Large clusters, lightweight scraping | lowest |
| **Standard** | Daily monitoring, balanced | medium |
| **Full** | Full observability, small/medium clusters | highest |

```bash
cv4pve-metrics-exporter --host=YOUR_HOST --api-token=... run           # Standard (default)
cv4pve-metrics-exporter --host=YOUR_HOST --api-token=... run --fast    # Fast
cv4pve-metrics-exporter --host=YOUR_HOST --api-token=... run --full    # Full
```

<details>
<summary><strong>Profiles comparison</strong></summary>

| Setting | Fast | Standard | Full |
|---------|:----:|:--------:|:----:|
| **Cluster** | | | |
| HA state | ✓ (no cache) | ✓ (no cache) | ✓ (cache 30s) |
| BackupInfo | ✓ (no cache) | ✓ (cache 10m) | ✓ (cache 10m) |
| **Node** | | | |
| Status (memory/swap/load/uptime + version) | | ✓ | ✓ |
| Subscription | | ✓ (cache 1h) | ✓ (cache 1h) |
| Replication | | ✓ (no cache) | ✓ (cache 1m) |
| DiskSmart | | | ✓ (cache 10m) |
| **Guest** | | | |
| Balloon (1 RPC per running QEMU) | | | ✓ |
| **Other** | | | |
| API instrumentation | | ✓ | ✓ |

> Fast profile skips all per-node calls — good for very large clusters where scrape latency matters more than per-node detail.

</details>

---

## Features

- **Lock visibility** — `cv4pve_guest_lock{state}` exploded series for clean alerting (backup, snapshot, migrate, …)
- **Subscription monitoring** — info, exploded status and next due date
- **HA state** — exploded series for both guests (`cv4pve_ha_state`) and nodes (`cv4pve_ha_node_state`), plus `cv4pve_ha_quorate`
- **Replication on every node** — full cluster coverage
- **SMART disk health** — wearout and health per disk (opt-in, cached)
- **Node version per node** — `cv4pve_node_version_info` with version/release/repoid
- **Overcommit detection** — `cv4pve_node_cpu_assigned_cores` and `cv4pve_node_memory_assigned_bytes`
- **Backup compliance** — per-guest `cv4pve_not_backed_up_info` + cluster-wide `cv4pve_guests_not_backed_up`
- **API instrumentation** — per-endpoint duration histogram and error counter, with normalized paths
- **Self-monitoring** — `cv4pve_scrape_duration_seconds` + `cv4pve_scrape_errors_total{section}`
- **Per-collector cache TTL** — slow-changing data (SMART, subscription) cached to minimize Proxmox API load
- **Native C#** — single binary, cross-platform (Linux, Windows, macOS), no runtime dependencies
- **Native service support** — `systemd` (`Type=notify`) and Windows Services (no wrapper required)
- **Profile-driven** — `Fast`/`Standard`/`Full` profiles or full `settings.json` for fine-grained control

---

## Installation

| Platform | Command |
|----------|---------|
| **Linux** | `wget .../cv4pve-metrics-exporter-linux-x64.zip && unzip cv4pve-metrics-exporter-linux-x64.zip && chmod +x cv4pve-metrics-exporter` |
| **Windows WinGet** | `winget install Corsinvest.cv4pve.metrics-exporter` |
| **Windows manual** | Download `cv4pve-metrics-exporter-win-x64.zip` from [Releases](https://github.com/Corsinvest/cv4pve-metrics-exporter/releases) |
| **Arch Linux** | `yay -S cv4pve-metrics-exporter` |
| **Debian/Ubuntu** | `sudo dpkg -i cv4pve-metrics-exporter-VERSION-ARCH.deb` |
| **RHEL/Fedora** | `sudo rpm -i cv4pve-metrics-exporter-VERSION-ARCH.rpm` |
| **macOS Homebrew** | `brew install corsinvest/tap/cv4pve-metrics-exporter` |
| **macOS manual** | `wget .../cv4pve-metrics-exporter-osx-x64.zip && unzip cv4pve-metrics-exporter-osx-x64.zip && chmod +x cv4pve-metrics-exporter` |

All binaries on the [Releases page](https://github.com/Corsinvest/cv4pve-metrics-exporter/releases).

### Required Permissions

| Permission | Purpose | Scope |
|------------|---------|-------|
| **VM.Audit** | Read VM/CT configuration and status | Virtual machines |
| **Datastore.Audit** | Read storage metrics | Storage systems |
| **Sys.Audit** | Node status, disks, subscription, replication | Cluster nodes |

### Create an API Token

```bash
# Create dedicated user (recommended)
pveum user add metrics@pve

# Grant read-only permissions
pveum aclmod / -user metrics@pve -role PVEAuditor

# Create token (save the secret — shown only once!)
pveum user token add metrics@pve metrics --privsep 0
```

---

## Running as a Service

The binary is **service-aware out of the box** — it integrates natively with both `systemd` (Linux) and Windows SCM. Run it interactively during development, then promote the same binary to a managed service in production without any wrapper.

### Linux (systemd)

Supports `Type=notify` — systemd is informed when the exporter is ready and gets proper graceful shutdown on `systemctl stop`.

```ini
# /etc/systemd/system/cv4pve-metrics-exporter.service
[Unit]
Description=Proxmox VE Metrics Exporter for Prometheus
After=network-online.target
Wants=network-online.target

[Service]
Type=notify
User=prometheus
Group=prometheus
ExecStart=/usr/local/bin/cv4pve-metrics-exporter \
  --host=pve.local \
  --api-token=metrics@pve!metrics=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx \
  --settings-file=/etc/cv4pve/metrics-exporter.json \
  run
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
# Create a dedicated unprivileged user (optional)
sudo useradd -r -s /bin/false prometheus

# Place the settings file somewhere the service user can read
sudo install -d /etc/cv4pve
sudo install -m 640 -o root -g prometheus settings.json /etc/cv4pve/metrics-exporter.json

sudo systemctl daemon-reload
sudo systemctl enable --now cv4pve-metrics-exporter
sudo systemctl status cv4pve-metrics-exporter
sudo journalctl -u cv4pve-metrics-exporter -f
```

> **Note:** always use **absolute paths** for `--settings-file` — systemd does not set a working directory by default.

### Windows (native service)

The binary integrates with Windows SCM directly.

```powershell
# Install as a Windows service
sc.exe create cv4pve-metrics-exporter `
  binPath= "C:\Tools\cv4pve-metrics-exporter\cv4pve-metrics-exporter.exe --host=pve.local --api-token=metrics@pve!metrics=xxx --settings-file=C:\ProgramData\cv4pve\metrics-exporter.json run" `
  start= auto `
  DisplayName= "Corsinvest Proxmox VE Metrics Exporter"

# Start / stop
sc.exe start cv4pve-metrics-exporter
sc.exe stop cv4pve-metrics-exporter

# View logs in Event Viewer → Windows Logs → Application
```

> **Note on quoting:** `sc.exe` is picky — the space after `binPath=` and around each `=` is required.

### Docker

Run the binary in a minimal container — no special flags needed, SIGTERM is handled.

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
COPY cv4pve-metrics-exporter /usr/local/bin/
EXPOSE 9221
ENTRYPOINT ["/usr/local/bin/cv4pve-metrics-exporter"]
CMD ["run"]
```

```bash
docker run -d --name pve-metrics \
  -p 9221:9221 \
  -v /etc/cv4pve:/config:ro \
  cv4pve-metrics-exporter \
  --host=pve.local \
  --api-token=metrics@pve!metrics=xxx \
  --settings-file=/config/metrics-exporter.json \
  run
```

Remember to bind to `0.0.0.0` (via `Host` in `settings.json`) when exposing outside the container.

### Prometheus scrape config

```yaml
scrape_configs:
  - job_name: proxmox
    static_configs:
      - targets: ['exporter-host:9221']
```

---

## Settings Reference

Customize the exporter by creating and editing a `settings.json` file:

```bash
# Step 1 — generate a settings file (pick your starting profile)
cv4pve-metrics-exporter create-settings          # Standard (default)
cv4pve-metrics-exporter create-settings --fast   # Fast
cv4pve-metrics-exporter create-settings --full   # Full

# Step 2 — edit settings.json to your needs

# Step 3 — run with your custom settings
cv4pve-metrics-exporter --host=YOUR_HOST --api-token=... --settings-file=settings.json run
```

Each collector exposes two knobs:
- `Enabled` — turn the collector on/off
- `CacheSeconds` — TTL of the cached result (0 = always refresh)

Cache is the recommended way to keep slow-changing data (SMART, subscription, backup info) up-to-date without hammering the Proxmox API on every scrape.

<details>
<summary><strong>Full settings.json with all defaults (Standard profile)</strong></summary>

```jsonc
{
  "Prometheus": {
    "Enabled": true,                    // enable the Prometheus exporter
    "Host": "localhost",                // HTTP listener host (0.0.0.0 to expose publicly)
    "Port": 9221,                       // HTTP listener port
    "Url": "metrics/",                  // HTTP URL path
    "MaxParallelRequests": 5,           // parallel API requests per scrape
    "ApiInstrumentation": true,         // per-endpoint API duration histogram + errors counter
    "Cluster": {
      "Ha":         { "Enabled": true,  "CacheSeconds": 0 },
      "BackupInfo": { "Enabled": true,  "CacheSeconds": 600 }
    },
    "Node": {
      "Status":       { "Enabled": true,  "CacheSeconds": 0 },     // memory/swap/load + version
      "Subscription": { "Enabled": true,  "CacheSeconds": 3600 },  // status + next due
      "Replication":  { "Enabled": true,  "CacheSeconds": 0 },
      "DiskSmart":    { "Enabled": false, "CacheSeconds": 0 }      // opt-in (1 call per node)
    },
    "Guest": {
      "Balloon":      { "Enabled": false, "CacheSeconds": 0 }      // opt-in (1 RPC per running QEMU)
    }
  }
}
```

</details>

---

## Response Files

Arguments can be stored in a response file and referenced with `@filename`. This is useful to avoid repeating connection parameters on every run.

```text
# config.rsp
--host
192.168.1.1
--api-token
user@pam!metrics=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

```bash
cv4pve-metrics-exporter @config.rsp run
cv4pve-metrics-exporter @config.rsp --settings-file=settings.json run
cv4pve-metrics-exporter @config.rsp run --full
```

- One token per line (option name and value on separate lines)
- Lines starting with `#` are comments
- Response files can be nested: a line starting with `@` references another file

---

## Exported Metrics

All metrics are prefixed with `cv4pve_`.

<details>
<summary><strong>Show all exported metrics</strong></summary>

### Core (always on)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_up` | gauge | `id`, `type` | 1 if resource is online/running/available |
| `cv4pve_cluster_info` | gauge | `name`, `version` | Cluster info (always 1) |
| `cv4pve_cluster_quorate` | gauge | `name` | 1 if cluster is quorate |
| `cv4pve_cluster_nodes` | gauge | `name` | Number of nodes in the cluster |
| `cv4pve_node_info` | gauge | `id`, `name`, `ip`, `level` | Node info (always 1) |
| `cv4pve_guest_info` | gauge | `id`, `vmid`, `node`, `name`, `type`, `tags`, `template` | VM/CT info (always 1, `tags` sorted to prevent churn) |
| `cv4pve_guest_lock` | gauge | `id`, `state` | 1 if guest matches lock state — `backup`/`clone`/`create`/`migrate`/`rollback`/`snapshot`/`snapshot-delete`/`suspended`/`suspending` |
| `cv4pve_storage_info` | gauge | `id`, `node`, `storage`, `content` | Storage info (always 1, `content` is sorted CSV) |
| `cv4pve_storage_shared` | gauge | `id` | 1 if storage is shared across nodes |

### Self-monitoring (always on)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_scrape_duration_seconds` | gauge | — | Duration of the last scrape |
| `cv4pve_scrape_last_success_timestamp_seconds` | gauge | — | Unix timestamp of the last successful scrape |
| `cv4pve_scrape_errors_total` | counter | `section` | Total number of errors per scrape section |

### Guest (per VM/CT, always on)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_guest_cpu_usage_ratio` | gauge | `id` | CPU usage (0..1) |
| `cv4pve_guest_cpu_cores` | gauge | `id` | CPU cores allocated |
| `cv4pve_guest_memory_size_bytes` | gauge | `id` | Configured memory |
| `cv4pve_guest_memory_usage_bytes` | gauge | `id` | Used memory |
| `cv4pve_guest_memory_host_ratio` | gauge | `id` | Guest memory usage over host total (0..1) |
| `cv4pve_guest_disk_size_bytes` | gauge | `id` | Disk total size |
| `cv4pve_guest_disk_usage_bytes` | gauge | `id` | Disk used |
| `cv4pve_guest_uptime_seconds` | gauge | `id` | Uptime |
| `cv4pve_guest_disk_read_bytes_total` | counter | `id` | Total bytes read |
| `cv4pve_guest_disk_write_bytes_total` | counter | `id` | Total bytes written |
| `cv4pve_guest_network_receive_bytes_total` | counter | `id` | Total bytes received |
| `cv4pve_guest_network_transmit_bytes_total` | counter | `id` | Total bytes transmitted |

### Storage (per storage, always on)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_storage_size_bytes` | gauge | `id` | Storage total size |
| `cv4pve_storage_usage_bytes` | gauge | `id` | Storage used |

### Node Status (if `Node.Status` enabled)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_node_uptime_seconds` | gauge | `node` | Uptime |
| `cv4pve_node_load_avg1` | gauge | `node` | Load average 1 min |
| `cv4pve_node_load_avg5` | gauge | `node` | Load average 5 min |
| `cv4pve_node_load_avg15` | gauge | `node` | Load average 15 min |
| `cv4pve_node_memory_used_bytes` | gauge | `node` | Memory used |
| `cv4pve_node_memory_total_bytes` | gauge | `node` | Memory total |
| `cv4pve_node_memory_assigned_bytes` | gauge | `node` | Sum of configured memory of running guests on this node |
| `cv4pve_node_swap_used_bytes` | gauge | `node` | Swap used |
| `cv4pve_node_swap_total_bytes` | gauge | `node` | Swap total |
| `cv4pve_node_root_fs_used_bytes` | gauge | `node` | Root FS used |
| `cv4pve_node_root_fs_total_bytes` | gauge | `node` | Root FS total |
| `cv4pve_node_cpu_assigned_cores` | gauge | `node` | Sum of CPU cores allocated to running guests |
| `cv4pve_node_version_info` | gauge | `node`, `version`, `release`, `repoid` | Node Proxmox VE version (always 1) |

### Subscription (if `Node.Subscription` enabled)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_node_subscription_info` | gauge | `node`, `level` | Subscription info (always 1) |
| `cv4pve_node_subscription_status` | gauge | `node`, `status` | 1 if matches status — `active`/`expired`/`new`/`notfound`/`invalid`/`suspended` |
| `cv4pve_node_subscription_next_due_timestamp_seconds` | gauge | `node` | Next due date as Unix timestamp |

### Replication (if `Node.Replication` enabled)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_replication_duration_seconds` | gauge | `id`, `type`, `source`, `target`, `guest` | Last replication duration |
| `cv4pve_replication_last_sync_timestamp_seconds` | gauge | same | Last successful sync |
| `cv4pve_replication_next_sync_timestamp_seconds` | gauge | same | Next scheduled sync |
| `cv4pve_replication_failed_total` | counter | same | Failed replication count |

### SMART (if `Node.DiskSmart` enabled)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_node_disk_health` | gauge | `node`, `serial`, `type`, `dev_path` | 1 if SMART PASSED |
| `cv4pve_node_disk_wearout` | gauge | same | Wearout percentage (0..100) |

### HA (always on)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_ha_state` | gauge | `sid`, `type`, `group`, `state` | 1 if guest matches state |
| `cv4pve_ha_node_state` | gauge | `node`, `state` | 1 if node matches HA state |
| `cv4pve_ha_quorate` | gauge | — | 1 if the HA manager reports quorum |

Guest `state` values: `stopped`, `request_stop`, `request_start`, `request_start_balance`, `started`, `fence`, `recovery`, `migrate`, `relocate`, `freeze`, `error`, `disabled`.
Node `state` values: `online`, `maintenance`, `unknown`, `fence`, `gone`.

### Backup (always on)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_guests_not_backed_up` | gauge | — | Number of guests not covered by any backup job |
| `cv4pve_not_backed_up_info` | gauge | `id` | 1 if guest is not covered by any backup job |

### Balloon (if `Guest.Balloon` enabled)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_guest_balloon_actual_bytes` | gauge | `id`, `vmid` | QEMU balloon actual memory |

### API Instrumentation (if `ApiInstrumentation = true`)

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `cv4pve_api_request_duration_seconds` | histogram | `method`, `endpoint` | Duration of Proxmox API requests (path is normalized: `{node}`, `{vmid}`, `{upid}`) |
| `cv4pve_api_request_errors_total` | counter | `method`, `endpoint` | Failed Proxmox API requests |

</details>

---

## Performance Tuning

### Increase parallelism

By default the exporter runs up to **5 parallel API requests** per scrape (`MaxParallelRequests = 5`).

```jsonc
"MaxParallelRequests": 10
```

> **Don't go too high.** Each parallel request is a real HTTP call to Proxmox. Values between 5 and 15 are a reasonable range. On very large clusters, prefer the `Fast` profile or aggressive cache TTL over increasing parallelism.

### Cache TTL for slow-changing data

Set `CacheSeconds` per collector to avoid hammering Proxmox on every scrape:

```jsonc
"Node": {
  "DiskSmart":    { "Enabled": true, "CacheSeconds": 1800 },  // 30 min
  "Subscription": { "Enabled": true, "CacheSeconds": 3600 }   // 1 h
}
```

The metric remains visible to Prometheus between refreshes — only the underlying API call is skipped.

### Minimize API calls

The `Fast` profile skips all per-node toggles (Status, Subscription, Replication) — only the cluster-wide bulk calls plus HA. Ideal for very large clusters or high scrape frequencies.

### Debug API endpoints

Enable `ApiInstrumentation` (default on) and look at `cv4pve_api_request_duration_seconds` to identify which endpoints are slowest:

```promql
# Average latency per endpoint over last 5 min
rate(cv4pve_api_request_duration_seconds_sum[5m])
  / rate(cv4pve_api_request_duration_seconds_count[5m])

# p99 latency
histogram_quantile(0.99, rate(cv4pve_api_request_duration_seconds_bucket[5m]))
```

### Summary

| Setting | Effect | Default |
|---------|--------|---------|
| `MaxParallelRequests` ↑ | Faster, but more load on Proxmox | 5 |
| `*.CacheSeconds` ↑ | Fewer API calls for slow data, metrics held between refresh | 0 / 600 / 3600 (varies) |
| `ApiInstrumentation` | Per-endpoint latency histograms — handy for tuning | on |
| `Fast` profile | Skip all per-node calls | off |

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
