import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 1400, height: 900 });

await page.goto('http://localhost:5173', { waitUntil: 'networkidle', timeout: 10000 });

// Lessons
const lessonsBtn = await page.locator('button').filter({ hasText: 'שיעורים' }).first();
await lessonsBtn.click();
await page.waitForTimeout(1200);
await page.screenshot({ path: '/tmp/lessons-page.png' });
console.log('✓ Lessons');

// Back to home -> Play
await page.click('text=קרא את הגרף');
await page.waitForTimeout(600);

const playBtn = await page.locator('button').filter({ hasText: 'משחק' }).first();
await playBtn.click();
await page.waitForTimeout(1500);
await page.screenshot({ path: '/tmp/play-page.png' });
console.log('✓ Play');

// Practice
await page.click('text=תרגול');
await page.waitForTimeout(900);
await page.screenshot({ path: '/tmp/practice-page.png' });
console.log('✓ Practice');

await browser.close();
