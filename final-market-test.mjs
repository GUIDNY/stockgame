import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();

await page.goto('https://chart-game-pi.vercel.app', { 
  waitUntil: 'domcontentloaded',
  timeout: 10000 
});

const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    await btn.click();
    break;
  }
}

// Wait for updates
for (let i = 0; i < 5; i++) {
  await page.waitForTimeout(1000);
  const cards = await page.locator('.ticker-card').count();
  if (cards > 0) {
    console.log(`✓ Found ${cards} ticker cards`);
    break;
  }
}

await page.screenshot({ path: '/tmp/market-final.png' });
console.log('✓ Screenshot ready');

await browser.close();
