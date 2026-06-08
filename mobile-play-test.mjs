import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 375, height: 812 });

await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });

// Click Play button
const playBtn = await page.locator('button').filter({ hasText: 'משחק' }).first();
const visible = await playBtn.isVisible();

if (!visible) {
  // Click hamburger first
  const hamburger = await page.locator('.nav-hamburger').first();
  await hamburger.click();
  await page.waitForTimeout(400);
}

const playBtn2 = await page.locator('button').filter({ hasText: 'משחק' }).first();
await playBtn2.click();
await page.waitForTimeout(2000);

await page.screenshot({ path: '/tmp/mobile-play-polish.png' });
console.log('✓ Mobile Play page screenshot');

await browser.close();
