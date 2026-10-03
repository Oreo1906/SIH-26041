// Build-time conversion only. The installed worker app has no icon download path.
const fs = require('fs');
const path = require('path');
const sharp = require(process.env.SURAKSHA_SHARP_PATH || 'sharp');
const root = path.resolve(__dirname, '..');
const sourceDirectory = path.join(root, 'third_party', 'material-symbols');
const outputDirectory = path.join(root, 'mobile-unity', 'Assets', 'SurakshaXR', 'Resources', 'UIIcons');
const sources = JSON.parse(fs.readFileSync(path.join(sourceDirectory, 'sources.json'), 'utf8').replace(/^\uFEFF/, ''));
(async () => {
  for (const source of sources) {
    let svg = fs.readFileSync(path.join(sourceDirectory, source.name + '.svg'), 'utf8');
    svg = svg.replace(/\sfill="(?!none)[^"]*"/g, '').replace('<svg ', '<svg fill="#ffffff" ');
    await sharp(Buffer.from(svg)).resize(96, 96).png({ compressionLevel: 9 }).toFile(path.join(outputDirectory, source.name + '.png'));
  }
  console.log(`Rasterized ${sources.length} bundled white 96px icons; UI Toolkit applies local tint.`);
})().catch(error => { console.error(error.message); process.exitCode = 1; });
