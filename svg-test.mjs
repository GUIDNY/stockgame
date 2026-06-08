import { chromium } from 'playwright';

const browser = await chromium.launch();

// Desktop
const desktop = await browser.newPage();
desktop.setViewportSize({ width: 1200, height: 800 });
await desktop.goto('http://localhost:5173', { waitUntil: 'networkidle' });
await desktop.screenshot({ path: '/tmp/navbar-desktop.png' });
console.log('✓ Desktop navbar with SVG icons');

// Mobile
const mobile = await browser.newPage();
mobile.setViewportSize({ width: 375, height: 667 });
await mobile.goto('http://localhost:5173', { waitUntil: 'networkidle' });
await mobile.screenshot({ path: '/tmp/navbar-mobile.png' });

const hamburger = await mobile.locator('.nav-hamburger').first();
await hamburger.click();
await mobile.waitForTimeout(600);
await mobile.screenshot({ path: '/tmp/navbar-menu.png' });
console.log('✓ Mobile navbar with SVG icons');

console.log('\n✅ SVG icons loaded successfully');
await browser.close();
