import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();

await page.goto('https://chart-game-pi.vercel.app', { waitUntil: 'networkidle' });

const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    await btn.click();
    break;
  }
}

await page.waitForTimeout(4000);

// Check what's in market-content
const content = await page.locator('.market-content').innerHTML();
console.log('Market content HTML:');
console.log(content.substring(0, 500));

// Check if loading div exists
const loadingDiv = await page.locator('.market-loading').count();
console.log(`\nLoading div count: ${loadingDiv}`);

// Check grid
const gridDiv = await page.locator('.ticker-grid').count();
console.log(`Grid div count: ${gridDiv}`);

await browser.close();
