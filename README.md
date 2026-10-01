# <img src="icon.png" alt="" height="36" align="top"> cv4pve-metrics-exporter

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

> **Prometheus exporter for Proxmox VE**: one endpoint for the whole cluster: nodes, VMs, containers and storage, plus HA, replication, guests without a backup, locks, subscription and SMART.
>
> **[Documentation](https://corsinvest.github.io/cv4pve-metrics-exporter/)**
>
> Prefer not to run a separate service? cv4pve-metrics-exporter also runs inside [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin), as its [Metrics Exporter](https://corsinvest.github.io/cv4pve-admin/modules/metrics-exporter/) module.

---

## Why

Proxmox VE can send node, guest and storage usage to an external metric server. What keeps a cluster healthy is often elsewhere: whether the HA manager has quorum and every resource is started, whether replication still syncs, which VMs no backup job includes, which guest has been locked since last night's backup, when the subscription expires, which SSD is wearing out.

cv4pve-metrics-exporter reads all of it and publishes it in the Prometheus format. Prometheus keeps the history and turns it into alerts.

It **runs outside the nodes and uses only the Proxmox VE API**: nothing to install on the cluster, no SSH, a read-only `PVEAuditor` token.

---

## What it exports

A few lines of `/metrics` on a two-node cluster:

```
cv4pve_up{id="node/pve01",type="node"} 1
cv4pve_cluster_quorate{name="pve-cluster"} 1
cv4pve_node_memory_assigned_bytes{node="pve01"} 76235669504
cv4pve_guest_info{id="qemu/1006",vmid="1006",node="pve01",name="dc01",type="qemu",tags="domain-controller;prod",template="0"} 1
cv4pve_guest_lock{id="qemu/1006",state="backup"} 0
cv4pve_storage_usage_bytes{id="storage/pve01/datapool"} 1092213195072
cv4pve_replication_last_sync_timestamp_seconds{id="1006-0",type="local",source="pve01",target="pve02",guest="1006"} 1790683214
cv4pve_not_backed_up_info{id="qemu/203"} 1
cv4pve_ha_quorate 1
cv4pve_node_disk_wearout{node="pve01",serial="S3Z9NX0M412345",type="ssd",dev_path="/dev/sde"} 98
```

Every metric, label and unit is described in [Metrics](https://corsinvest.github.io/cv4pve-metrics-exporter/metrics/).

---

## Features

- **The whole cluster in one scrape**: nodes, VMs, containers and storages from two cluster-wide API calls, whatever the number of guests.
- **What the built-in metrics miss**: HA state of resources and nodes, replication, backup coverage, guest locks, subscription expiry, SMART health and SSD wearout.
- **Overcommit**: vCPUs and memory assigned to the running guests of each node.
- **Clear when Proxmox VE fails**: HTTP 503 when no node is reachable or the login fails, so Prometheus marks the target down; a single failed call is counted and the other metrics are still exported.
- **Light on the cluster**: `--fast` and `--full` profiles, a cache per collector, a limit on parallel calls.
- **Runs as a service**: systemd with `Type=notify`, native Windows service, no wrapper.

---

## Quick start

```bash
# Windows
winget install Corsinvest.cv4pve.metrics-exporter

# Linux (other platforms and packages: see the documentation)
wget https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/latest/download/cv4pve-metrics-exporter-linux-x64.zip
unzip cv4pve-metrics-exporter-linux-x64.zip && chmod +x cv4pve-metrics-exporter

# Run against any node of the cluster, with an API token
./cv4pve-metrics-exporter --host=pve1.local --api-token='metrics@pve!metrics=<uuid>' run
curl http://localhost:9221/metrics/
```

Then add the exporter to `prometheus.yml`, see [Prometheus](https://corsinvest.github.io/cv4pve-metrics-exporter/prometheus/). The API token needs only the `PVEAuditor` role: see [Permissions](https://corsinvest.github.io/cv4pve-metrics-exporter/permissions/).

---

## Documentation

| | |
|---|---|
| [Getting started](https://corsinvest.github.io/cv4pve-metrics-exporter/getting-started/) | Install, connect, run, first scrape |
| [Permissions](https://corsinvest.github.io/cv4pve-metrics-exporter/permissions/) | Creating the user and API token, privileges of each call |
| [Connection](https://corsinvest.github.io/cv4pve-metrics-exporter/connection/) | Connection options, options in a file for the service |
| [Prometheus](https://corsinvest.github.io/cv4pve-metrics-exporter/prometheus/) | Scrape configuration, listening address, how a scrape reads the cluster |
| [Run as a service](https://corsinvest.github.io/cv4pve-metrics-exporter/service/) | systemd on Linux, Windows service |
| [Metrics](https://corsinvest.github.io/cv4pve-metrics-exporter/metrics/) | Every metric, label and unit, and the setting that turns it on |
| [Settings](https://corsinvest.github.io/cv4pve-metrics-exporter/settings/) | Profiles, settings file, cache, performance |
| [AI assistants](https://corsinvest.github.io/cv4pve-metrics-exporter/ai-agents/) | Claude Code, Codex, the `cv4pve-metrics-exporter` skill |
| [Troubleshooting](https://corsinvest.github.io/cv4pve-metrics-exporter/troubleshooting/) | What happens when Proxmox VE fails, startup errors, debug output |

---

## Related tools

Use `cv4pve-metrics-exporter` to watch *how the cluster is doing*, [cv4pve-report](https://github.com/Corsinvest/cv4pve-report) to know *what you have*, [cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag) to know *what is wrong*. The whole suite: [corsinvest.it/cv4pve](https://www.corsinvest.it/en/cv4pve/).

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/en/cv4pve/).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
