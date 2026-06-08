const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  page.setViewportSize({ width: 1400, height: 900 });
  
  try {
    await page.goto('http://localhost:5173', { waitUntil: 'networkidle', timeout: 10000 });
    await page.screenshot({ path: '/tmp/home-page.png' });
    console.log('✓ Home page screenshot saved');
    
    await page.click('button:has-text("תבניות")');
    await page.waitForTimeout(800);
    await page.screenshot({ path: '/tmp/learn-page.png' });
    console.log('✓ Learn page screenshot saved');
    
    await page.click('button:has-text("שיעורים")');
    await page.waitForTimeout(800);
    await page.screenshot({ path: '/tmp/lessons-page.png' });
    console.log('✓ Lessons page screenshot saved');
    
  } catch (e) {
    console.error('Error:', e.message);
  } finally {
    await browser.close();
  }
})();
