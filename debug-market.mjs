import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();

// Log all console messages
page.on('console', msg => console.log(`[${msg.type()}] ${msg.text()}`));
page.on('pageerror', err => console.error(`[ERROR] ${err}`));

await page.goto('https://chart-game-pi.vercel.app', { waitUntil: 'networkidle' });

// Click Market
const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    console.log('✓ Clicking Market button...');
    await btn.click();
    break;
  }
}

await page.waitForTimeout(2000);

// Check if market page content exists
const marketTitle = await page.locator('.market-title').count();
console.log(`Market title elements: ${marketTitle}`);

const tickerCards = await page.locator('.ticker-card').count();
console.log(`Ticker cards found: ${tickerCards}`);

const marketContent = await page.locator('.market-content').count();
console.log(`Market content div: ${marketContent}`);

// Get page HTML snippet
const html = await page.locator('.market-page').innerHTML();
console.log(`Market page HTML length: ${html.length}`);
if (html.length < 200) console.log(`HTML: ${html}`);

await browser.close();
