import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 1400, height: 900 });

try {
  await page.goto('http://localhost:5173', { waitUntil: 'networkidle', timeout: 10000 });
  await page.screenshot({ path: '/tmp/home-page.png' });
  console.log('✓ Home page');
  
  const learnBtn = await page.$('button:has-text("תבניות")') || await page.locator('button').filter({ hasText: 'תבניות' }).first();
  if (learnBtn) await learnBtn.click();
  await page.waitForTimeout(800);
  await page.screenshot({ path: '/tmp/learn-page.png' });
  console.log('✓ Learn page');
} catch (e) {
  console.error('Error:', e.message);
} finally {
  await browser.close();
}
