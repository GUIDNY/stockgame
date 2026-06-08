import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();

page.on('console', msg => {
  if (msg.type() === 'error') console.log(`[ERROR] ${msg.text()}`);
});

await page.goto('https://chart-game-pi.vercel.app', { waitUntil: 'networkidle' });

const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    await btn.click();
    break;
  }
}

// Wait for component to update
await page.waitForTimeout(3000);

const marketTitle = await page.locator('.market-title').textContent();
console.log(`✓ Title: ${marketTitle}`);

const tickerCount = await page.locator('.ticker-card').count();
console.log(`✓ Ticker cards: ${tickerCount}`);

if (tickerCount > 0) {
  const firstCard = await page.locator('.ticker-card').first();
  const ticker = await firstCard.locator('.tc-ticker').textContent();
  const price = await firstCard.locator('.tc-price').textContent();
  console.log(`✓ First card: ${ticker} ${price}`);
}

await page.screenshot({ path: '/tmp/market-fixed.png' });
console.log('✓ Screenshot saved');

await browser.close();
