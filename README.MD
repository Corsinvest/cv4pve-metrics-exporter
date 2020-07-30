# cv4pve-metrics-export

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-metrics-exporter.svg)](LICENSE.md) [![AppVeyor branch](https://img.shields.io/appveyor/ci/franklupo/cv4pve-metrics-exporter/master.svg)](https://ci.appveyor.com/project/franklupo/cv4pve-metrics-exporter)

```text
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Metrics Exporter for Proxmox VE         (Made in Italy)

Usage: cv4pve-metrics-exporter [command] [options]

Options:
  -?|-h|--help      Show help information
  --version         Show version information
  --host            The host name host[:port],host1[:port],host2[:port]
  --api-token       Api token format 'USER@REALM!TOKENID=UUID'. Require Proxmox VE 6.2 or later
  --username        User name <username>@<realm>
  --password        The password. Specify 'file:path_file' to store password in file.

Commands:
  app-check-update  Check update application
  app-upgrade       Upgrade application
  prometheus        Export for Prometheus

Run 'cv4pve-metrics-exporter [command] -?|-h|--help' for more information about a command.

cv4pve-metrics-exporter is a part of suite cv4pve-tools.
For more information visit https://www.cv4pve-tools.com
```

## Copyright and License

Copyright: Corsinvest Srl
For licensing details please visit [LICENSE.md](LICENSE.md)

## Commercial Support

This software is part of a suite of tools called cv4pve-tools. If you want commercial support, visit the [site](https://www.cv4pve-tools.com)

## Introduction

Metrics Exporter for Proxmox VE.

this software export metrics from Proxmox VE.

## Main features

* Completely written in C#
* Use native api REST Proxmox VE (library C#)
* Independent os (Windows, Linux, Macosx)
* Installation unzip file extract binary
* Not require installation in Proxmox VE
* Execute out side Proxmox VE
* Use Api token --api-token parameter

## Api token

From version 6.2 of Proxmox VE is possible to use [Api token](https://pve.proxmox.com/pve-docs/pveum-plain.html).
This feature permit execute Api without using user and password.
If using **Privilege Separation** when create api token remember specify in permission.

## Configuration

E.g. install on linux 64

Download last package e.g. Debian cv4pve-metrics-exporter-linux-x64.zip, on your os and install:

```sh
root@debian:~# unzip cv4pve-metrics-exporter-linux-x64.zip
```

This tool need basically no configuration.

```sh
root@debian:~# cv4pve-metrics-exporter --host=192.168.0.100 --username=root@pam --password=fagiano prometheus

Corsinvest for Proxmox VE
Cluster: 192.168.0.100 - User: root
Exporter Prometheus: http://localhost:9221/metrics/
```

## Prometheus

For Prometheus are available more parameters:

```sh
--http-host   Http host (default: localhost)
--http-port   Http port (default: 9221)
--http-url    Http url (default: metrics/)
--prefix      Prefix export (default: cv4pve)
```
