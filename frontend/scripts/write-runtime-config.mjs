import { writeFileSync } from 'node:fs';

const rawOrigin = (process.env.API_ORIGIN ?? '').trim().replace(/\/+$/, '');
if (rawOrigin) {
  const origin = new URL(rawOrigin);
  if (!['http:', 'https:'].includes(origin.protocol) || origin.origin !== rawOrigin) {
    throw new Error('API_ORIGIN must be an HTTP(S) origin without a path.');
  }
}

const target = new URL('../public/runtime-config.js', import.meta.url);
writeFileSync(target, `window.__CENTRAL_CONTABIL_CONFIG__={apiOrigin:${JSON.stringify(rawOrigin)}};\n`);
