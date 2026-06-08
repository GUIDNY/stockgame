import { chromium } from 'playwright';

const browser = await chromium.launch();

// Desktop
const desktopPage = await browser.newPage();
desktopPage.setViewportSize({ width: 1200, height: 800 });
await desktopPage.goto('http://localhost:5173', { waitUntil: 'networkidle' });
await desktopPage.screenshot({ path: '/tmp/desktop.png' });
console.log('✓ Desktop screenshot');

// Mobile
const mobilePage = await browser.newPage();
mobilePage.setViewportSize({ width: 375, height: 667 });
await mobilePage.goto('http://localhost:5173', { waitUntil: 'networkidle' });
await mobilePage.screenshot({ path: '/tmp/mobile-home.png' });

// Click hamburger
const hamburger = await mobilePage.locator('.nav-hamburger').first();
await hamburger.click();
await mobilePage.waitForTimeout(500);
await mobilePage.screenshot({ path: '/tmp/mobile-menu.png' });
console.log('✓ Mobile screenshots');

await browser.close();
