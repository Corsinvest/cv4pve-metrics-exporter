// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-metrics-exporter',
  integrations: [
    starlight({
      title: 'cv4pve-metrics-exporter',
      description: 'Prometheus exporter for Proxmox VE: nodes, VMs, containers, storage, HA, replication, backup coverage, subscription and SMART, from outside the cluster.',
      // Brand, product icon, GitHub link, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-metrics-exporter',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Button in the home hero: the same engine runs inside cv4pve-admin.
          admin: { module: 'metrics-exporter' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 12 },
          // Steps panel in the home hero: the same steps, in the same order and words, as
          // Getting started (CliGettingStarted). The commands are in the pages (CliInstall).
          steps: {
            items: [
              'Install cv4pve-metrics-exporter',
              { text: 'Create an API token', href: 'permissions/#user-and-token' },
              'Run `cv4pve-metrics-exporter run`',
              'Scrape the metrics',
            ],
          },
        }),
      ],
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions', 'connection', 'ai-agents', 'troubleshooting'],
        },
        {
          label: 'Integration',
          items: ['prometheus', 'service'],
        },
        {
          label: 'Metrics',
          items: [
            { label: 'Overview', slug: 'metrics' },
            'metrics/cluster-nodes',
            'metrics/guests',
            'metrics/storage',
            'metrics/ha',
            'metrics/replication-backup',
            'metrics/exporter',
          ],
        },
        {
          label: 'Reference',
          items: ['settings'],
        },
      ],
    }),
  ],
});
