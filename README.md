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

---

## Quick Start

```bash
# Download latest release
wget https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/download/VERSION/cv4pve-metrics-exporter-linux-x64.zip
unzip cv4pve-metrics-exporter-linux-x64.zip
chmod +x cv4pve-metrics-exporter

# Run Prometheus exporter
./cv4pve-metrics-exporter --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD prometheus
```

---

## Features

### Core Capabilities

#### **Performance & Reliability**
- **Native C#** implementation
- **Cross-platform** (Windows, Linux, macOS)
- **API-based** operation (no root access required)
- **Cluster support** with automatic resource resolution
- **High availability** with multiple host support

#### **Prometheus Integration**
- **Full metrics export** for monitoring and alerting
- **Customizable endpoint** (host, port, URL path)
- **Rich metrics** covering nodes, VMs, containers, storage, and replication
- **Label-based organization** for easy filtering and querying

#### **Enterprise Features**
- **API token** support (Proxmox VE 6.2+)
- **SSL validation** options
- **Multiple host** support for HA
- **Error resilience** with comprehensive logging
- **Service mode** for production deployments

---

## Installation

### Linux Installation

```bash
# Check available releases and get the specific version number
# Visit: https://github.com/Corsinvest/cv4pve-metrics-exporter/releases

# Download specific version (replace VERSION with actual version like v1.5.0)
wget https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/download/VERSION/cv4pve-metrics-exporter-linux-x64.zip

# Alternative: Get latest release URL programmatically
LATEST_URL=$(curl -s https://api.github.com/repos/Corsinvest/cv4pve-metrics-exporter/releases/latest | grep browser_download_url | grep linux-x64 | cut -d '"' -f 4)
wget "$LATEST_URL"

# Extract and make executable
unzip cv4pve-metrics-exporter-linux-x64.zip
chmod +x cv4pve-metrics-exporter

# Optional: Move to system path
sudo mv cv4pve-metrics-exporter /usr/local/bin/
```

### Windows Installation

```powershell
# Check available releases at: https://github.com/Corsinvest/cv4pve-metrics-exporter/releases
# Download specific version (replace VERSION with actual version)
Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/download/VERSION/cv4pve-metrics-exporter-win-x64.zip" -OutFile "cv4pve-metrics-exporter.zip"

# Extract
Expand-Archive cv4pve-metrics-exporter.zip -DestinationPath "C:\Tools\cv4pve-metrics-exporter"

# Add to PATH (optional)
$env:PATH += ";C:\Tools\cv4pve-metrics-exporter"
```

### macOS Installation

```bash
# Check available releases at: https://github.com/Corsinvest/cv4pve-metrics-exporter/releases
# Download specific version (replace VERSION with actual version)
wget https://github.com/Corsinvest/cv4pve-metrics-exporter/releases/download/VERSION/cv4pve-metrics-exporter-osx-x64.zip
unzip cv4pve-metrics-exporter-osx-x64.zip
chmod +x cv4pve-metrics-exporter

# Move to applications
sudo mv cv4pve-metrics-exporter /usr/local/bin/
```

---

## Configuration

### Command Line Options

```text
Usage:
  cv4pve-metrics-exporter [options] [command]

Options:
  --host <host> (REQUIRED)    The host name host[:port],host1[:port],host2[:port]
  --username <username>       User name <username>@<realm>
  --password <password>       The password. Specify 'file:path_file' to store password in file.
  --api-token <api-token>     Api token format 'USER@REALM!TOKENID=UUID'. Require Proxmox VE 6.2 or later
  --validate-certificate      Validate SSL Certificate Proxmox VE node.
  --service-mode              Run as background service (runs until stopped, no Enter key)
  --version                   Show version information
  -?, -h, --help              Show help and usage information

Commands:
  prometheus  Export for Prometheus
```

### Prometheus Command Options

```text
Options:
  --http-host <http-host>    Http host (default: localhost)
  --http-port <http-port>    Http port (default: 9221)
  --http-url <http-url>      Http url (default: metrics/)
  --prefix <prefix>          Prefix export (default: cv4pve)
```

### Usage Examples

```bash
# Basic usage with username/password
cv4pve-metrics-exporter --host=192.168.0.100 --username=root@pam --password=YOUR_PASSWORD prometheus

# Using API token (recommended)
cv4pve-metrics-exporter --host=192.168.0.100 --api-token=metrics-user@pve!metrics=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx prometheus

# Multiple hosts for HA
cv4pve-metrics-exporter --host=pve1.local:8006,pve2.local:8006,pve3.local:8006 --username=root@pam --password=YOUR_PASSWORD prometheus

# Custom Prometheus endpoint
cv4pve-metrics-exporter --host=192.168.0.100 --username=root@pam --password=YOUR_PASSWORD prometheus --http-host=0.0.0.0 --http-port=9090 --http-url=pve-metrics/

# Using password from file
echo "YOUR_PASSWORD" > /etc/cv4pve/password
cv4pve-metrics-exporter --host=192.168.0.100 --username=root@pam --password=file:/etc/cv4pve/password prometheus

# With SSL certificate validation
cv4pve-metrics-exporter --host=192.168.0.100 --username=root@pam --password=YOUR_PASSWORD --validate-certificate prometheus
```

---

## Deployment Options

### Console Mode (Default)

Run the exporter in interactive mode. The exporter will wait for you to press Enter to stop.

```bash
./cv4pve-metrics-exporter --host=pve.local --username=root@pam --password=YOUR_PASSWORD prometheus
# Metrics exporter is running. Press Enter to stop...
```

**Best for:** Development, testing, and debugging.

### Service Mode

Run the exporter as a background service without console interaction. The exporter will continue running until stopped with Ctrl+C.

```bash
./cv4pve-metrics-exporter --host=pve.local --username=root@pam --password=YOUR_PASSWORD --service-mode prometheus
# Exporter is running in background. Press Ctrl+C to stop.
```

**Best for:** Production environments where you want the exporter to run continuously.

### Windows Service with NSSM

For Windows systems, use NSSM (Non-Sucking Service Manager) to run the exporter as a proper Windows service.

```powershell
# Download NSSM: https://nssm.cc/download
# Install the service
nssm install cv4pve-metrics-exporter "C:\Tools\cv4pve-metrics-exporter\cv4pve-metrics-exporter.exe" `
  --host=pve.local `
  --api-token=metrics@pve!metrics=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx `
  --service-mode `
  prometheus `
  --http-host=0.0.0.0

# Start the service
nssm start cv4pve-metrics-exporter

# View logs
nssm tail cv4pve-metrics-exporter

# Stop the service
nssm stop cv4pve-metrics-exporter

# Uninstall the service
nssm remove cv4pve-metrics-exporter confirm
```

### Linux Systemd Service

For Linux systems, create a systemd service file.

```bash
# Create service file
sudo nano /etc/systemd/system/cv4pve-metrics-exporter.service
```

Add the following configuration:

```ini
[Unit]
Description=Proxmox VE Metrics Exporter for Prometheus
After=network.target

[Service]
Type=simple
ExecStart=/usr/local/bin/cv4pve-metrics-exporter --host=pve.local --api-token=metrics@pve!metrics=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx --service-mode prometheus --http-host=0.0.0.0
Restart=always
RestartSec=10
User=prometheus
Group=prometheus

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
# Create user
sudo useradd -r -s /bin/false prometheus

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable cv4pve-metrics-exporter
sudo systemctl start cv4pve-metrics-exporter

# Check status
sudo systemctl status cv4pve-metrics-exporter

# View logs
sudo journalctl -u cv4pve-metrics-exporter -f
```

### Service Mode vs Console Mode

| Feature | Console Mode (Default) | Service Mode (`--service-mode`) |
|---------|------------------------|--------------------------------|
| **Interactive Stop** | ✅ Press Enter to stop | ❌ Use Ctrl+C or system stop |
| **Console Output** | ✅ Full logging to console | ✅ Full logging to console |
| **Runs in Background** | ❌ Waits for Enter key | ✅ Runs until stopped |
| **Systemd Integration** | ⚠️ Limited | ✅ Full support with auto-restart |
| **NSSM Integration** | ⚠️ Limited | ✅ Full support with auto-restart |
| **Error Handling** | ✅ Exits on fatal errors | ✅ Exits on fatal errors |
| **Best For** | Development, Testing | Production, Services |

---

## Exported Metrics

The exporter provides comprehensive metrics about your Proxmox VE environment. All metrics use the configurable prefix (default: `cv4pve`).

### Status & Availability Metrics

#### `cv4pve_up`
Resource availability status (1 = available/running, 0 = unavailable/stopped).

**Labels:**
- `id` - Resource identifier (node name, storage ID, or VM/CT ID)
- `type` - Resource type: `node`, `storage`, `qemu` (VM), `lxc` (container)

**Example:**
```
cv4pve_up{id="pve1",type="node"} 1
cv4pve_up{id="100",type="qemu"} 1
cv4pve_up{id="local-lvm",type="storage"} 1
```

---

### Cluster Metrics

#### `cv4pve_cluster_info`
Cluster-wide information. Value is always 1 when cluster is accessible.

**Labels:**
- `name` - Cluster name
- `nodes` - Total number of nodes
- `quorate` - Quorum status (1 = quorate, 0 = not quorate)
- `version` - Proxmox VE version

**Example:**
```
cv4pve_cluster_info{name="proxmox",nodes="3",quorate="1",version="8.2"} 1
```

---

### Node Metrics

#### `cv4pve_node_info`
Node information and status. Value is always 1.

**Labels:**
- `name` - Node name
- `ip` - Node IP address
- `level` - Support level
- `local` - Is local node (true/false)
- `online` - Node online status (true/false)
- `status` - Node status

**Example:**
```
cv4pve_node_info{name="pve1",ip="192.168.1.10",level="c",local="true",online="true",status="online"} 1
```

#### `cv4pve_node_subscription_info`
Node subscription status. Value is 1 when subscription is active, 0 otherwise.

**Labels:**
- `node` - Node name
- `status` - Subscription status (e.g., "active", "inactive", "notfound")
- `level` - Support level (e.g., "community", "basic", "standard", "premium")

**Example:**
```
cv4pve_node_subscription_info{node="pve1",status="active",level="standard"} 1
```

**Note:** This metric provides subscription visibility, which is not available in prometheus-pve-exporter.

#### `cv4pve_node_disk_smart_health`
Disk SMART health status from wearout indicator.

**Labels:**
- `node` - Node name
- `disk` - Disk device name (e.g., "/dev/sda")

**Example:**
```
cv4pve_node_disk_smart_health{node="pve1",disk="/dev/sda"} 98
```

#### `cv4pve_node_disk_smart_wearout`
Disk wear level percentage from SMART data.

**Labels:**
- `node` - Node name
- `disk` - Disk device name

**Example:**
```
cv4pve_node_disk_smart_wearout{node="pve1",disk="/dev/nvme0n1"} 5
```

#### `cv4pve_node_load_avg1`, `cv4pve_node_load_avg5`, `cv4pve_node_load_avg15`
Node load averages over 1, 5, and 15 minutes.

**Labels:**
- `id` - Node name

**Example:**
```
cv4pve_node_load_avg1{id="pve1"} 0.45
cv4pve_node_load_avg5{id="pve1"} 0.38
cv4pve_node_load_avg15{id="pve1"} 0.42
```

#### `cv4pve_node_uptime_seconds`
Node uptime in seconds.

**Labels:**
- `id` - Node name

#### Node Memory Metrics
- `cv4pve_node_memory_used_bytes` - Used memory
- `cv4pve_node_memory_total_bytes` - Total memory
- `cv4pve_node_memory_free_bytes` - Free memory

**Labels:**
- `id` - Node name

#### Node Swap Metrics
- `cv4pve_node_swap_used_bytes` - Used swap space
- `cv4pve_node_swap_total_bytes` - Total swap space
- `cv4pve_node_swap_free_bytes` - Free swap space

**Labels:**
- `id` - Node name

#### Node Root Filesystem Metrics
- `cv4pve_node_root_fs_used_bytes` - Used root filesystem space
- `cv4pve_node_root_fs_total_bytes` - Total root filesystem space
- `cv4pve_node_root_fs_free_bytes` - Free root filesystem space

**Labels:**
- `id` - Node name

---

### Guest (VM/CT) Metrics

#### `cv4pve_guest_info`
Guest (VM or container) information. Value is always 1.

**Labels:**
- `id` - VM/CT ID
- `name` - VM/CT name
- `node` - Node hosting the guest
- `type` - Guest type: `qemu` (VM) or `lxc` (container)
- `status` - Current status (e.g., "running", "stopped")
- `tags` - Tags assigned to the guest
- `lock` - **Lock status** (e.g., "backup", "snapshot", "migrate", or empty if not locked)

**Example:**
```
cv4pve_guest_info{id="100",name="webserver",node="pve1",type="qemu",status="running",tags="production",lock=""} 1
cv4pve_guest_info{id="101",name="database",node="pve2",type="qemu",status="running",tags="production",lock="backup"} 1
```

**Note:** The `lock` label shows why a VM/CT is locked (backup in progress, snapshot being created, etc.). This enhanced visibility is unique to cv4pve-metrics-exporter.

#### CPU Metrics
- `cv4pve_cpu_usage_ratio` - CPU usage ratio (0.0 to 1.0)
- `cv4pve_cpu_usage_limit` - CPU limit (number of cores)

**Labels:**
- `id` - VM/CT ID

#### Memory Metrics
- `cv4pve_memory_size_bytes` - Configured memory size
- `cv4pve_memory_usage_bytes` - Current memory usage

**Labels:**
- `id` - VM/CT ID

#### Disk Metrics
- `cv4pve_disk_size_bytes` - Total disk size
- `cv4pve_disk_usage_bytes` - Current disk usage
- `cv4pve_disk_read_bytes` - Total bytes read
- `cv4pve_disk_write_bytes` - Total bytes written

**Labels:**
- `id` - VM/CT ID

#### Network Metrics
- `cv4pve_network_transmit_bytes` - Total bytes transmitted
- `cv4pve_network_receive_bytes` - Total bytes received

**Labels:**
- `id` - VM/CT ID

#### `cv4pve_uptime_seconds`
Guest uptime in seconds.

**Labels:**
- `id` - VM/CT ID

#### Balloon Memory Metrics (QEMU VMs only)
- `cv4pve_balloon_actual_bytes` - Actual balloon memory
- `cv4pve_balloon_max_mem_bytes` - Maximum balloon memory
- `cv4pve_balloon_last_update_bytes` - Last balloon update value

**Labels:**
- `id` - VM ID

**Note:** Balloon memory allows dynamic memory management for QEMU VMs.

#### `cv4pve_host_memory_usage_bytes`
Host memory usage for guest.

**Labels:**
- `id` - VM/CT ID

#### `cv4pve_onboot_status`
Guest auto-start configuration. Value is 1 when onboot is enabled, 0 otherwise.

**Labels:**
- `id` - VM/CT ID

---

### Storage Metrics

#### `cv4pve_storage_info`
Storage information. Value is always 1.

**Labels:**
- `id` - Storage identifier
- `node` - Node name (for local storage) or empty for shared storage
- `shared` - Is shared storage (true/false)
- `enabled` - Is storage enabled (true/false)
- `active` - Is storage active (true/false)
- `disk_size` - **Total disk size in bytes** (e.g., "1099511627776" for 1TB)
- `disk_usage` - **Used disk space in bytes**

**Example:**
```
cv4pve_storage_info{id="local-lvm",node="pve1",shared="false",enabled="true",active="true",disk_size="500000000000",disk_usage="250000000000"} 1
cv4pve_storage_info{id="nfs-shared",node="",shared="true",enabled="true",active="true",disk_size="2000000000000",disk_usage="1200000000000"} 1
```

**Note:** Including disk size and usage in labels (rather than separate metrics) allows for better grouping and filtering in Prometheus queries. This is an enhanced feature of cv4pve-metrics-exporter.

---

### High Availability (HA) Metrics

#### `cv4pve_ha_resource_info`
High Availability resource information. Value is 1 when resource is started, 0 otherwise.

**Labels:**
- `sid` - Service ID (format: `vm:ID` or `ct:ID`)
- `state` - HA state (e.g., "started", "stopped", "disabled")
- `type` - Resource type: `vm` or `ct`
- `group` - HA group name (if assigned)

**Example:**
```
cv4pve_ha_resource_info{sid="vm:100",state="started",type="vm",group="ha-group1"} 1
cv4pve_ha_resource_info{sid="ct:101",state="stopped",type="ct",group=""} 0
```

**Note:** HA resource monitoring is unique to cv4pve-metrics-exporter, allowing you to monitor HA configurations and state in Prometheus.

---

### Replication Metrics

#### `cv4pve_replication_duration_seconds`
Duration of last replication job in seconds.

**Labels:**
- `guest` - Guest ID (VM/CT)
- `id` - Replication job ID
- `jobnum` - Job number
- `source` - Source node
- `target` - Target node

#### `cv4pve_replication_last_sync_timestamp_seconds`
Timestamp of last successful replication sync (Unix timestamp).

**Labels:**
- `guest` - Guest ID
- `id` - Replication job ID
- `jobnum` - Job number
- `source` - Source node
- `target` - Target node

#### `cv4pve_replication_last_try_timestamp_seconds`
Timestamp of last replication attempt (Unix timestamp).

**Labels:**
- `guest` - Guest ID
- `id` - Replication job ID
- `jobnum` - Job number
- `source` - Source node
- `target` - Target node

#### `cv4pve_replication_next_sync_timestamp_seconds`
Timestamp of next scheduled replication sync (Unix timestamp).

**Labels:**
- `guest` - Guest ID
- `id` - Replication job ID
- `jobnum` - Job number
- `source` - Source node
- `target` - Target node

#### `cv4pve_replication_failed_syncs`
Number of failed replication syncs.

**Labels:**
- `guest` - Guest ID
- `id` - Replication job ID
- `jobnum` - Job number
- `source` - Source node
- `target` - Target node

**Example:**
```
cv4pve_replication_duration_seconds{guest="100",id="100-0",jobnum="0",source="pve1",target="pve2"} 45.2
cv4pve_replication_failed_syncs{guest="100",id="100-0",jobnum="0",source="pve1",target="pve2"} 0
```

---

### Key Differences from prometheus-pve-exporter

cv4pve-metrics-exporter provides several enhancements over the standard prometheus-pve-exporter:

1. **Lock Status Visibility**: The `lock` label in `cv4pve_guest_info` shows why a VM/CT is locked (backup, snapshot, migrate, etc.)

2. **Subscription Monitoring**: `cv4pve_node_subscription_info` tracks subscription status and support level for each node

3. **Storage in Labels**: Disk size and usage are included as labels in `cv4pve_storage_info` for better query flexibility

4. **HA Resource Monitoring**: `cv4pve_ha_resource_info` provides visibility into High Availability configurations

5. **SMART Disk Health**: Comprehensive disk health metrics including wearout indicators

6. **Native C# Implementation**: Better performance and cross-platform support

7. **Service Mode**: Built-in support for running as a system service with proper lifecycle management

8. **Flexible Deployment**: Single binary with no runtime dependencies

---

## API Token Setup

For Proxmox VE 6.2 and later, using API tokens is recommended:

### Create API Token

1. Log into Proxmox VE web interface
2. Go to **Datacenter** → **Permissions** → **API Tokens**
3. Click **Add**
4. Fill in the details:
   - **User**: `root@pam` (or create dedicated user)
   - **Token ID**: `metrics`
   - **Privilege Separation**: Uncheck for full permissions (or configure specific permissions)
5. Click **Add**
6. **Save the token secret** - it will only be shown once!

### Required Permissions

If using privilege separation, the token needs:

- **Path**: `/`
- **Role**: `PVEAuditor` (read-only access)

```bash
# Create dedicated user for metrics (optional)
pveum user add metrics-user@pve

# Create API token
pveum user token add metrics-user@pve metrics --privsep 0

# Or with privilege separation (more secure)
pveum user token add metrics-user@pve metrics --privsep 1
pveum aclmod / -user metrics-user@pve -role PVEAuditor
```

---

## Troubleshooting

### Connection Issues

```bash
# Test connection to Proxmox VE API
curl -k https://YOUR_HOST:8006/api2/json/version

# Check if exporter is running
curl http://localhost:9221/metrics/

# Verify API token
pveum user token list
```

### Common Issues

**Issue**: `Connection refused`
- Check firewall rules on Proxmox VE host (port 8006)
- Verify network connectivity: `ping YOUR_HOST`

**Issue**: `SSL certificate validation failed`
- Use `--validate-certificate` only with valid SSL certificates
- For self-signed certificates, omit this flag

**Issue**: `Permission denied`
- Verify API token or user has sufficient permissions
- Check token hasn't expired: `pveum user token list`

**Issue**: `Metrics not appearing in Prometheus`
- Verify exporter endpoint: `curl http://localhost:9221/metrics/`
- Check Prometheus scrape configuration
- Review Prometheus logs: `journalctl -u prometheus -f`

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
