import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();

await page.goto('http://localhost:5173', { waitUntil: 'networkidle', timeout: 10000 });

const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    console.log('Clicking Market...');
    await btn.click();
    break;
  }
}

await page.waitForTimeout(3000);

const cards = await page.locator('.ticker-card').count();
console.log(`Ticker cards: ${cards}`);

if (cards > 0) {
  const firstCard = await page.locator('.ticker-card').first();
  const ticker = await firstCard.locator('.tc-ticker').textContent();
  const price = await firstCard.locator('.tc-price').textContent();
  const change = await firstCard.locator('.tc-change').textContent();
  console.log(`✓ ${ticker} ${price} ${change}`);
}

await page.screenshot({ path: '/tmp/local-market.png' });
console.log('✓ Screenshot saved');

await browser.close();
