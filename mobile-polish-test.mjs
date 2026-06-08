import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 375, height: 812 });

await page.goto('http://localhost:5173/play', { waitUntil: 'networkidle' });
await page.waitForTimeout(1500);

await page.screenshot({ path: '/tmp/mobile-polish.png' });
console.log('✓ Mobile polish screenshot captured');

await browser.close();
