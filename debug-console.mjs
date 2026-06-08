import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();

const logs = [];
const errors = [];

page.on('console', msg => {
  const t = msg.text();
  if (msg.type() === 'error') errors.push(t);
  else if (msg.type() === 'log') logs.push(t);
});

page.on('pageerror', err => {
  errors.push(`Page Error: ${err}`);
});

await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });

const buttons = await page.locator('button').all();
for (const btn of buttons) {
  const txt = await btn.textContent();
  if (txt.includes('שוק')) {
    await btn.click();
    break;
  }
}

await page.waitForTimeout(2000);

console.log('=== LOGS ===');
logs.forEach(l => console.log(l));
console.log('\n=== ERRORS ===');
errors.forEach(e => console.log(e));

const html = await page.locator('.market-page').innerHTML();
console.log('\n=== MARKET HTML ===');
console.log(html.substring(0, 600));

await browser.close();
