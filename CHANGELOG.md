# Changelog

## [Unreleased]

### Breaking

- `cv4pve_replication_failed_total` (counter) is replaced by `cv4pve_replication_fail_count` (gauge). Proxmox VE reports the failed attempts *in a row*, back to 0 at the first success: as a counter it never went down, and new failures after a recovery were not seen. Alert on `cv4pve_replication_fail_count > 0`.

### Fixed

- Each scrape answered with the data of the previous one, and the first scrape after start was empty.
- Concurrent API calls could receive each other's answers (the shared `LastResult` of the API client): each call now uses its own client, sharing connection and login.
- A failed login or an unreachable cluster stopped the exporter. The scrape now answers HTTP 503, so Prometheus sets `up` to 0, and the exporter keeps running.
- Series of objects that no longer exist were exported forever: a deleted guest or storage, a renamed guest, old tags, the previous version of an upgraded node, a guest later covered by a backup job, an offline node's per-node metrics.
- Guest disk and network counters stayed frozen after a guest restarted or migrated; they now reset, and `rate()` works.
- HA: `cv4pve_ha_node_state` never reported the node states, and `cv4pve_ha_state` did not use the states of the HA manager. Both now come from `/cluster/ha/status/manager_status`.
- SMART: SAS disks, which report `OK` instead of `PASSED`, were exported as unhealthy.
- The standard profile did not cache backup coverage (10 min) and subscription (1 h); a setting left out of the settings file now takes its standard value.
- A call that failed was cached like a successful one and not retried until the cache expired.
- Failed backup-coverage and balloon calls were not counted in `cv4pve_scrape_errors_total` (balloon: `section="guest"`), and `cv4pve_scrape_last_success_timestamp_seconds` advanced even when calls failed.
- `--debug` and `--log-level` did not log the Proxmox VE API calls.
- Under systemd without a `WorkingDirectory` the exporter hung at start (a file watcher on `/`); `systemctl stop` ended with exit code 143 and marked the unit failed; `Type=notify` reported ready before the endpoint was listening.
- When the HTTP endpoint could not start (port taken, address not allowed) the exporter exited with code 0, so `Restart=on-failure` did not restart it.
- As a Windows service, the log was not written anywhere; it now goes to the Event Log.
- Help text of `--settings-file`.

### Changed
- Documentation site: [corsinvest.github.io/cv4pve-metrics-exporter](https://corsinvest.github.io/cv4pve-metrics-exporter/), with every metric, alert rules and service setup; shorter README
- Updated Corsinvest.ProxmoxVE.Api.Extension and Api.Console to 9.2.3
- Product icon (Lucide `chart-spline`) and Windows executable icon
- Project metadata, symbols (Source Link, `.snupkg`) and code style aligned with the other cv4pve tools

## 2.0.0

Major release. Many metric names and settings have changed — read the **Migration** section below if you are upgrading.

### What's new

- **New `Full` profile for complete observability** — SMART disk health, QEMU balloon, subscription expiry, backup compliance all enabled with sensible cache intervals.
- **Cache per-collector** — slow-changing data (SMART, subscription) is refreshed in the background; Prometheus keeps seeing fresh metrics but your Proxmox API load drops dramatically. Configurable per collector.
- **Backup compliance metrics** — alert when a VM/CT has no backup job covering it (`cv4pve_not_backed_up_info`, `cv4pve_guests_not_backed_up`).
- **HA visibility for nodes** — not just guests: node HA state (online/maintenance/fence/...) and HA quorum status are exposed.
- **Subscription expiration tracking** — get alerted before your Proxmox subscription expires (`cv4pve_node_subscription_next_due_timestamp_seconds`).
- **Per-node Proxmox version** — see which node runs which version (useful for rolling-upgrade dashboards).
- **Overcommit detection** — total CPU cores / RAM allocated vs actually available, per node.
- **Cluster-wide replication** — previously only the local node's replication jobs were reported; now all nodes.
- **Per-endpoint API instrumentation** — built-in histograms tell you which Proxmox API call is slow or failing. Great for debugging on large clusters.
- **Native service support** — Linux systemd (`Type=notify`) and Windows Services work out of the box. No NSSM required on Windows.
- **Simpler CLI** — one `run` command plus `create-settings` to generate a starting `settings.json`.

### Migration

#### CLI

| 1.x | 2.0 |
|---|---|
| `cv4pve-metrics-exporter ... prometheus` | `cv4pve-metrics-exporter ... run` |
| `--http-host` / `--http-port` / `--http-url` | set them in `settings.json` |
| `--prefix=custom` | prefix is fixed to `cv4pve_` |
| `--service-mode` | not needed anymore — works as a service natively |

Generate a new `settings.json`:

```bash
cv4pve-metrics-exporter create-settings          # Standard profile
cv4pve-metrics-exporter create-settings --fast   # minimum API calls
cv4pve-metrics-exporter create-settings --full   # everything on, with cache
```

Run:

```bash
cv4pve-metrics-exporter --host=YOUR_HOST --api-token=... run --settings-file=settings.json
```

#### Settings file

The schema now nests every exporter's configuration under its name, and every collector is a small object with `Enabled` + `CacheSeconds`.

Before:

```jsonc
{
  "HttpHost": "localhost",
  "HttpPort": 9221,
  "Prefix": "cv4pve",
  "Node": { "Smart": false }
}
```

After:

```jsonc
{
  "Prometheus": {
    "Host": "localhost",
    "Port": 9221,
    "Node": {
      "DiskSmart": { "Enabled": false, "CacheSeconds": 0 }
    }
  }
}
```

The easiest path: delete your old file and run `create-settings --full` to get the new structure.

#### Dashboards & alerts

Many metric names changed to follow Prometheus naming conventions. Update your queries accordingly:

**Guest metrics (VM/CT) now have a `guest_` prefix**:

| 1.x | 2.0 |
|---|---|
| `cv4pve_cpu_usage_ratio` | `cv4pve_guest_cpu_usage_ratio` |
| `cv4pve_cpu_usage_limit` | `cv4pve_guest_cpu_cores` |
| `cv4pve_memory_size_bytes` | `cv4pve_guest_memory_size_bytes` |
| `cv4pve_memory_usage_bytes` | `cv4pve_guest_memory_usage_bytes` |
| `cv4pve_disk_size_bytes` | `cv4pve_guest_disk_size_bytes` |
| `cv4pve_disk_usage_bytes` | `cv4pve_guest_disk_usage_bytes` |
| `cv4pve_disk_read_bytes_total` | `cv4pve_guest_disk_read_bytes_total` |
| `cv4pve_disk_write_bytes_total` | `cv4pve_guest_disk_write_bytes_total` |
| `cv4pve_network_receive_bytes_total` | `cv4pve_guest_network_receive_bytes_total` |
| `cv4pve_network_transmit_bytes_total` | `cv4pve_guest_network_transmit_bytes_total` |
| `cv4pve_uptime_seconds` | `cv4pve_guest_uptime_seconds` |
| `cv4pve_balloon_actual_bytes` | `cv4pve_guest_balloon_actual_bytes` |
| `cv4pve_host_memory_usage_bytes` | `cv4pve_guest_memory_host_ratio` (is actually 0..1, renamed accordingly) |

**Other renames**:

- `cv4pve_node_disk_Wearout` → `cv4pve_node_disk_wearout` (lowercase)
- `cv4pve_replication_failed_syncs` → `cv4pve_replication_failed_total` (counter)

**Removed metrics** (replaced by equivalents):

- `cv4pve_guest_template` — use `cv4pve_guest_info{template="1"}` instead.
- `cv4pve_node_memory_free_bytes` / `swap_free_bytes` / `root_fs_free_bytes` — compute as `total - used`.
- `cv4pve_replication_last_try_timestamp_seconds` — use `last_sync` + `failed_total`.
- `cv4pve_balloon_max_mem_bytes` — same as `cv4pve_guest_memory_size_bytes`.
- `cv4pve_balloon_last_update_bytes` — was not really "bytes", removed.
- `cv4pve_onboot_status` — removed (rarely actionable, too many API calls on large clusters).

**Moved from label to exploded series**:

- Guest lock state: was `cv4pve_guest_info{lock="backup"} 1`, now `cv4pve_guest_lock{id="qemu/100", state="backup"} 1`.
  Use it in alerts: `cv4pve_guest_lock{state="backup"} == 1`.
- Storage shared flag: was a label in `storage_info`, now `cv4pve_storage_shared{id}` (0/1).
- Cluster quorate: was a label in `cluster_info`, now `cv4pve_cluster_quorate{name}` (0/1).
- Node count: was a label in `cluster_info`, now `cv4pve_cluster_nodes{name}`.

**New labels in existing metrics**:

- `cv4pve_up` — added `type` label (`node`, `cluster`, `qemu`, `lxc`, `storage`).
- `cv4pve_guest_info` — added `template` label (replaces `cv4pve_guest_template`).
- `cv4pve_storage_info` — added `content` label (sorted, stable across scrapes).

**New metrics**:

- `cv4pve_guest_lock{id, state}` — guest lock state, exploded
- `cv4pve_cluster_quorate{name}` / `cv4pve_cluster_nodes{name}`
- `cv4pve_storage_shared{id}`
- `cv4pve_storage_size_bytes{id}` / `cv4pve_storage_usage_bytes{id}`
- `cv4pve_node_memory_assigned_bytes{node}` / `cv4pve_node_cpu_assigned_cores{node}` — overcommit visibility
- `cv4pve_node_version_info{node, version, release, repoid}`
- `cv4pve_node_subscription_status{node, status}` — exploded
- `cv4pve_node_subscription_next_due_timestamp_seconds{node}`
- `cv4pve_ha_node_state{node, state}` / `cv4pve_ha_quorate`
- `cv4pve_guests_not_backed_up` / `cv4pve_not_backed_up_info{id}`
- `cv4pve_scrape_errors_total{section}` — self-monitoring
- `cv4pve_api_request_duration_seconds{method, endpoint}` — per-endpoint histogram
- `cv4pve_api_request_errors_total{method, endpoint}`

### Platform support

- `systemd` service unit example available in the README; `Type=notify` supported.
- Windows: install as a native service with `sc.exe` — NSSM no longer needed.
- Docker examples in the README.
