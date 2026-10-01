---
name: cv4pve-metrics-exporter
description: Read the current state and usage of a Proxmox VE cluster from the Prometheus endpoint of cv4pve-metrics-exporter (nodes, VMs, containers, storage, HA, replication, backup coverage, SMART), and set the exporter up for Prometheus. Use it when the user asks for live values of the cluster, or about running, configuring or troubleshooting the exporter. It only reads the cluster.
---

# cv4pve-metrics-exporter

`cv4pve-metrics-exporter run` starts an HTTP endpoint and stays in the foreground. It has no timer: every
request to the endpoint reads the cluster through the Proxmox VE API and answers with the metrics in the
Prometheus text format. `PVEAuditor` is enough: it changes nothing on the cluster. The connection options
(host and API token) are in a file the user names: if you do not know its path, ask.

## Rules

- If the exporter already runs, as a service or elsewhere, only read its endpoint: ask the user for the
  address. Start one yourself only when none runs.
- `run` does not return: start it in the background, read the endpoint, then stop the process you started.
  Do not leave it running, and do not stop an exporter you did not start.
- Connect only with the options file, passed with `@`. Do not print it or copy the token anywhere.
- Every request reads the whole cluster: read the endpoint once into a file and query the file. Do not
  poll it in a loop.
- Request the host name of the settings, `localhost` by default: `127.0.0.1` gets `400` or `404`.
- `--settings-file` goes before `run`; `--fast` and `--full` after it. A settings file wins over them.
- Do not install or change a service, the Prometheus configuration or the listening address unless the
  user asks: show the change first.
- If an option is refused, check `cv4pve-metrics-exporter --help`: this skill can be newer than the tool.

## Run and read

```bash
cv4pve-metrics-exporter @<options-file> run &          # prints: Prometheus: http://localhost:9221/metrics/
curl -s http://localhost:9221/metrics/ -o metrics.txt  # one read of the cluster
kill %1                                                # stop the exporter you started
```

In PowerShell: `$p = Start-Process cv4pve-metrics-exporter -ArgumentList '@<options-file>','run' -PassThru`,
then `Stop-Process -Id $p.Id`.

| Answer | Cause |
|---|---|
| `503` | The exporter could not connect or log in to Proxmox VE |
| `400`, `404` | Wrong host name (use the one of the settings) or wrong path (`metrics/`) |
| Connection refused | The exporter is not running, or listens on another port |

## The metrics

```bash
grep -v '^#' metrics.txt | head                         # the values
grep '^# HELP' metrics.txt                              # every metric with its description
grep '^cv4pve_up' metrics.txt                           # what is online: 1 or 0
grep '^cv4pve_guest_info' metrics.txt                   # names, nodes and tags of the guests
```

- Resources are identified by the `id` label: `node/<node>`, `qemu/<vmid>`, `lxc/<vmid>`,
  `storage/<node>/<storage>`. Per-node metrics have a `node` label instead.
- Names, tags and versions are labels of the `…_info` metrics (`cv4pve_guest_info`, `cv4pve_node_info`,
  `cv4pve_storage_info`), whose value is always 1: match them to the other series by `id`.
- A state (guest lock, HA state, subscription) is one series per possible value with a `state` or `status`
  label: the current one has value 1.
- Profiles: standard by default; `--fast` only the cluster-wide calls; `--full` adds SMART and QEMU
  balloon. A metric that is missing may be off in the profile.

Every metric, label and unit: https://corsinvest.github.io/cv4pve-metrics-exporter/metrics/

Documentation: https://corsinvest.github.io/cv4pve-metrics-exporter/
