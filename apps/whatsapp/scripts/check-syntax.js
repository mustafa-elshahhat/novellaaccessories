import { readdirSync, statSync } from 'node:fs';
import { join, extname } from 'node:path';
import { execSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const root = join(__dirname, '..');

const srcDir = join(root, 'src');
const files = [];

function walk(dir) {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      walk(full);
    } else if (extname(full) === '.js') {
      files.push(full);
    }
  }
}

walk(srcDir);

let failed = 0;

for (const file of files) {
  try {
    execSync(`node --check "${file}"`, { stdio: 'pipe' });
  } catch {
    failed += 1;
    process.stderr.write(`FAIL  ${file}\n`);
  }
}

if (failed === 0) {
  process.stdout.write(`OK  (${files.length} files)\n`);
  process.exit(0);
} else {
  process.stderr.write(`FAILED  (${failed} of ${files.length} files)\n`);
  process.exit(1);
}
