import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage();
page.setViewportSize({ width: 375, height: 667 });

await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });

// Test 1: Home page
console.log('📱 Test 1: HOME PAGE');
let buttons = await page.locator('button').count();
console.log(`  Buttons: ${buttons}`);
await page.screenshot({ path: '/tmp/m1-home.png' });

// Test 2: Hamburger menu
console.log('\n📱 Test 2: MENU');
const hamburger = await page.locator('.nav-hamburger');
if (await hamburger.count() > 0) {
  console.log(`  Hamburger: ✓`);
  await hamburger.click();
  await page.waitForTimeout(600);
  await page.screenshot({ path: '/tmp/m2-menu.png' });
}

// Test 3: Market
console.log('\n📱 Test 3: MARKET');
const marketBtn = await page.locator('button').filter({ hasText: 'שוק' }).first();
await marketBtn.click();
await page.waitForTimeout(2000);
await page.screenshot({ path: '/tmp/m3-market.png' });
console.log(`  Loaded: ✓`);

// Test 4: Learn
console.log('\n📱 Test 4: LEARN');
const ham2 = await page.locator('.nav-hamburger').first();
await ham2.click();
await page.waitForTimeout(500);
const learnBtn = await page.locator('button').filter({ hasText: 'תבניות' }).first();
await learnBtn.click();
await page.waitForTimeout(1500);
await page.screenshot({ path: '/tmp/m4-learn.png' });
console.log(`  Loaded: ✓`);

// Test 5: Lessons
console.log('\n📱 Test 5: LESSONS');
const ham3 = await page.locator('.nav-hamburger').first();
await ham3.click();
await page.waitForTimeout(500);
const lessonsBtn = await page.locator('button').filter({ hasText: 'שיעורים' }).first();
await lessonsBtn.click();
await page.waitForTimeout(1500);
await page.screenshot({ path: '/tmp/m5-lessons.png' });
console.log(`  Loaded: ✓`);

// Test 6: Practice
console.log('\n📱 Test 6: PRACTICE');
const ham4 = await page.locator('.nav-hamburger').first();
await ham4.click();
await page.waitForTimeout(500);
const practiceBtn = await page.locator('button').filter({ hasText: 'תרגול' }).first();
await practiceBtn.click();
await page.waitForTimeout(1500);
await page.screenshot({ path: '/tmp/m6-practice.png' });
console.log(`  Loaded: ✓`);

// Test 7: Play
console.log('\n📱 Test 7: PLAY');
const ham5 = await page.locator('.nav-hamburger').first();
await ham5.click();
await page.waitForTimeout(500);
const playBtn = await page.locator('button').filter({ hasText: 'משחק' }).first();
await playBtn.click();
await page.waitForTimeout(2000);
await page.screenshot({ path: '/tmp/m7-play.png' });
console.log(`  Loaded: ✓`);

console.log('\n✅ MOBILE TESTS COMPLETE\n');
await browser.close();
