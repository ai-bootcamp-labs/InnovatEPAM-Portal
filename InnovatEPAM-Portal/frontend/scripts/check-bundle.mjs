#!/usr/bin/env node
// T112 — fail the build if the date-fns chunk exceeds the constitutional budget.
// We measure the gzipped size of every emitted JS asset that contains date-fns
// and assert the cumulative total stays under DATE_FNS_GZIP_LIMIT_BYTES.

import { readdir, readFile, stat } from 'node:fs/promises';
import { join } from 'node:path';
import { gzipSync } from 'node:zlib';

const DIST = new URL('../dist/assets/', import.meta.url).pathname;
const DATE_FNS_GZIP_LIMIT_BYTES = 20 * 1024;

async function listJsFiles(dir) {
  let entries;
  try {
    entries = await readdir(dir);
  } catch (err) {
    if (err.code === 'ENOENT') {
      console.error(`bundle-check: ${dir} does not exist — run \`npm run build\` first.`);
      process.exit(1);
    }
    throw err;
  }
  const out = [];
  for (const name of entries) {
    const full = join(dir, name);
    const info = await stat(full);
    if (info.isFile() && name.endsWith('.js')) out.push(full);
  }
  return out;
}

const files = await listJsFiles(DIST);
let dateFnsBytes = 0;
const matched = [];
for (const file of files) {
  const buf = await readFile(file);
  const text = buf.toString('utf8');
  if (text.includes('date-fns') || /[/\\]date-fns[/\\]/.test(text)) {
    const gz = gzipSync(buf).length;
    dateFnsBytes += gz;
    matched.push({ file, gz });
  }
}

if (matched.length === 0) {
  console.warn('bundle-check: no chunk contained the string "date-fns"; skipping budget assertion.');
  process.exit(0);
}

const human = (n) => `${(n / 1024).toFixed(2)} KB`;
console.log('bundle-check: chunks containing date-fns');
for (const m of matched) console.log(`  ${m.file}\n    gzip: ${human(m.gz)}`);
console.log(`bundle-check: total gzipped = ${human(dateFnsBytes)} (budget ${human(DATE_FNS_GZIP_LIMIT_BYTES)})`);

if (dateFnsBytes > DATE_FNS_GZIP_LIMIT_BYTES) {
  console.error(`bundle-check: FAIL — date-fns gzipped exceeds budget by ${human(dateFnsBytes - DATE_FNS_GZIP_LIMIT_BYTES)}.`);
  process.exit(1);
}
console.log('bundle-check: OK');
