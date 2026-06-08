import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 1400, height: 900 });

await page.goto('http://localhost:5173', { waitUntil: 'domcontentloaded', timeout: 10000 });

// Click Market button
const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    await btn.click();
    break;
  }
}

await page.waitForTimeout(3000);
await page.screenshot({ path: '/tmp/market-page2.png' });
console.log('✓ Market screenshot taken');

await browser.close();
