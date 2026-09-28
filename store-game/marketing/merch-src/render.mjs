import { createRequire } from 'module'; const require=createRequire('/opt/node22/lib/node_modules/');
const { chromium } = require('playwright'); import fs from 'fs';
const D='/tmp/claude-0/-home-user-stockgame/8ea923bd-9957-5d11-a1e4-8ee5779425eb/scratchpad/merch/';
const b=await chromium.launch({executablePath:'/opt/pw-browsers/chromium-1194/chrome-linux/chrome'});
const p=await b.newPage({viewport:{width:1600,height:900},deviceScaleFactor:1});
await p.goto('file://'+D+'all.html'); await p.waitForTimeout(500);
for(const id of fs.readFileSync(D+'ids.txt','utf8').split(' ')){ const el=await p.$('#'+id); await el.screenshot({path:D+id+'.png',omitBackground:true}); }
// contact sheet on the shop card colour
await p.setViewportSize({width:1600,height:420}); await p.addStyleTag({content:'body{background:#f4f5f9!important} .it{margin:4px;background:#f4f5f9;border-radius:24px} svg{width:190px;height:190px}'});
await p.screenshot({path:D+'sheet.png'}); await b.close();
