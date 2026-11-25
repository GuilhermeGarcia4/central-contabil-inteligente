import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const financeRoot = resolve(frontendRoot, 'src/app/features/finance');
const files = readdirSync(financeRoot).filter(file => file.endsWith('.ts'));
const missing = [];

for (const file of files) {
  const source = readFileSync(join(financeRoot, file), 'utf8');
  const references = [
    ...source.matchAll(/<img\b[^>]*\bsrc=["']([^"']+)["']/gi),
    ...source.matchAll(/(?<![A-Za-z])url\(["']?([^"')]+)["']?\)/gi)
  ].map(match => match[1]).filter(path => !/^(?:data:|https?:|#)/i.test(path));

  for (const reference of references) {
    const path = reference.split(/[?#]/, 1)[0];
    const candidates = path.startsWith('/')
      ? [resolve(frontendRoot, 'public', `.${path}`)]
      : [resolve(financeRoot, path), resolve(frontendRoot, 'src', path)];
    if (!candidates.some(existsSync)) missing.push(`${file}: ${reference}`);
  }
}

if (missing.length) {
  console.error(`Finance image assets not found:\n${missing.join('\n')}`);
  process.exitCode = 1;
} else {
  console.log(`Finance asset check passed (${files.length} component files scanned).`);
}
