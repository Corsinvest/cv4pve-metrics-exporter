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

The exporter provides comprehensive metrics about your Proxmox VE environment:

### Status Metrics
- `cv4pve_up` - Node/Storage/VM/CT online/running/available status

### Cluster Metrics
- `cv4pve_cluster_info` - Cluster information (nodes, quorate, version)

### Node Metrics
- `cv4pve_node_info` - Node information (IP, version, local status)
- `cv4pve_node_load_avg{1,5,15}` - Node load averages
- `cv4pve_node_uptime_seconds` - Node uptime
- `cv4pve_node_memory_{used,total,free}_bytes` - Node memory metrics
- `cv4pve_node_swap_{used,total,free}_bytes` - Node swap metrics
- `cv4pve_node_root_fs_{used,total,free}_bytes` - Node root filesystem metrics

### Guest (VM/CT) Metrics
- `cv4pve_guest_info` - Guest information (name, node, type, status, tags)
- `cv4pve_cpu_usage_ratio` - CPU usage
- `cv4pve_cpu_usage_limit` - CPU limit
- `cv4pve_memory_{size,usage}_bytes` - Memory metrics
- `cv4pve_disk_{size,usage,read,write}_bytes` - Disk metrics
- `cv4pve_network_{transmit,receive}_bytes` - Network metrics
- `cv4pve_uptime_seconds` - Guest uptime
- `cv4pve_balloon_{actual,max_mem,last_update}_bytes` - Balloon memory (QEMU only)
- `cv4pve_host_memory_usage_bytes` - Host memory usage
- `cv4pve_onboot_status` - Onboot configuration

### Storage Metrics
- `cv4pve_storage_info` - Storage information (node, shared status)

### Replication Metrics
- `cv4pve_replication_duration_seconds` - Replication duration
- `cv4pve_replication_last_sync_timestamp_seconds` - Last successful sync
- `cv4pve_replication_last_try_timestamp_seconds` - Last sync attempt
- `cv4pve_replication_next_sync_timestamp_seconds` - Next scheduled sync
- `cv4pve_replication_failed_syncs` - Failed sync count

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
