import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 375, height: 667 });

await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });

console.log('📱 HAMBURGER MENU TEST\n');

// Click hamburger
const hamburger = await page.locator('.nav-hamburger').first();
console.log('1. Hamburger visible:', await hamburger.count() > 0 ? '✓' : '✗');

await hamburger.click();
await page.waitForTimeout(600);

// Check menu
const menuOpen = await page.locator('.nav-links.open').count();
console.log('2. Menu opened:', menuOpen > 0 ? '✓' : '✗');

const menuItems = await page.locator('.nav-link').count();
console.log('3. Menu items count:', menuItems);

// Try clicking learn
const learnBtn = await page.locator('button').filter({ hasText: 'תבניות' }).first();
const btnVisible = await learnBtn.isVisible();
console.log('4. Learn button visible:', btnVisible ? '✓' : '✗');

if (btnVisible) {
  await learnBtn.click();
  await page.waitForTimeout(1000);
  const title = await page.locator('h1').first().textContent();
  console.log('5. Navigated to Learn:', title ? '✓' : '✗');
}

await page.screenshot({ path: '/tmp/menu-test.png' });
console.log('\n✅ Test complete');

await browser.close();
