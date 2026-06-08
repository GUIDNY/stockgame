import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 1400, height: 900 });

await page.goto('http://localhost:5173', { waitUntil: 'networkidle', timeout: 10000 });

// Click Market button
const marketBtn = await page.locator('button').filter({ hasText: 'שוק' }).first();
await marketBtn.click();
await page.waitForTimeout(2500); // Wait for prices to load

await page.screenshot({ path: '/tmp/market-page.png' });
console.log('✓ Market page screenshot');

await browser.close();
