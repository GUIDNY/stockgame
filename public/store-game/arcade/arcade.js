/* arcade.js: what the mall's two physics games share. A full-screen stage of its own (a canvas sized to the
   device's pixels, a bar with the way out), a little synthesiser for their sounds, particles, floating words, a
   shaking camera, and their saved progress. The games themselves are smash.js (a slingshot against towers of
   boxes guarded by full-price tags) and merge.js (drop products into a box; two the same become a dearer one).
   The mall opens them with BTArcade.open(id, host): host gives the products, pays the coins, and is told when
   the shopper leaves. Physics by matter.js 0.19 (MIT), loaded once beside this file. */
(function(){
const A=window.BTArcade=window.BTArcade||{games:{}};
const base=(document.currentScript&&document.currentScript.src||'').replace(/[^/]*$/,'');
A.base=base;
let MATTER=null;
A.matter=()=>MATTER||(MATTER=window.Matter?Promise.resolve(window.Matter):new Promise((res,rej)=>{ const s=document.createElement('script'); s.src=base+'matter.min.js'; s.onload=()=>res(window.Matter); s.onerror=()=>rej(new Error('matter.js')); document.head.appendChild(s); }));
const gameScript=id=>new Promise((res,rej)=>{ if(A.games[id]) return res(A.games[id]); const s=document.createElement('script'); s.src=base+id+'.js?v='+(A.v||1); s.onload=()=>A.games[id]?res(A.games[id]):rej(new Error(id)); s.onerror=()=>rej(new Error(id)); document.head.appendChild(s); });

// ---------- the stage ----------
const CSS=`.btarc{position:fixed;inset:0;z-index:80;background:#0d1030;display:flex;flex-direction:column;direction:rtl;font-family:Rubik,system-ui,sans-serif;color:#fff;touch-action:none;user-select:none;-webkit-user-select:none}
.btarc .bar{flex:none;display:flex;align-items:center;gap:8px;padding:calc(env(safe-area-inset-top,0px) + 8px) max(10px,env(safe-area-inset-right,0px)) 8px max(10px,env(safe-area-inset-left,0px));background:rgba(8,10,34,.72);position:absolute;top:0;left:0;right:0;z-index:2;pointer-events:none}
.btarc .bar>*{pointer-events:auto}
.btarc .bar b{flex:1;font-size:16px;font-weight:900;letter-spacing:.01em}
.btarc .bar button{height:38px;min-width:38px;padding:0 12px;border-radius:12px;border:0;background:rgba(255,255,255,.14);color:#fff;font:800 14px Rubik,system-ui,sans-serif}
.btarc .bar .sc{font-weight:900;font-variant-numeric:tabular-nums;background:rgba(255,255,255,.12);padding:7px 11px;border-radius:11px;font-size:15px}
.btarc canvas{flex:1;width:100%;height:100%;display:block;min-height:0}
.btarc .panel{position:absolute;inset:0;z-index:3;display:grid;grid-template-columns:minmax(0,1fr);place-items:center;background:rgba(6,8,28,.72);backdrop-filter:blur(3px);-webkit-backdrop-filter:blur(3px);padding:20px;overflow-y:auto}
.btarc .card{width:min(420px,calc(100vw - 32px));box-sizing:border-box;border:3px solid rgba(255,209,102,.55);background:linear-gradient(180deg,#2b3384,#141850);border-radius:22px;padding:22px 18px 18px;text-align:center;box-shadow:0 20px 60px rgba(0,0,0,.5),inset 0 1px 0 rgba(255,255,255,.12)}
.btarc .card h2{margin:0 0 6px;font-size:28px;font-weight:900;color:#ffd166;text-shadow:0 3px 0 rgba(0,0,0,.35)}
.btarc .card p{margin:6px 0;color:#c9cfee;font-size:15px;line-height:1.5}
.btarc .btn{display:inline-flex;align-items:center;justify-content:center;gap:6px;height:50px;padding:0 22px;border-radius:16px;border:0;font:900 17px Rubik,system-ui,sans-serif;color:#fff;background:linear-gradient(180deg,#ff7a2e,#e04a00);box-shadow:0 5px 0 #9e3300,0 10px 20px rgba(0,0,0,.35);cursor:pointer}
.btarc .btn:active{transform:translateY(3px);box-shadow:0 2px 0 #9e3300}
.btarc .btn.ghost{background:rgba(255,255,255,.12);box-shadow:none}
.btarc .row{display:flex;gap:10px;justify-content:center;flex-wrap:wrap;margin-top:14px}
.btarc .stars{font-size:46px;letter-spacing:6px;margin:4px 0}
.btarc .stars span{display:inline-block;filter:grayscale(1);opacity:.25;transform:scale(.6);transition:transform .35s cubic-bezier(.3,1.8,.5,1),opacity .2s}
.btarc .stars span.on{filter:none;opacity:1;transform:scale(1)}
.btarc .coins{font-size:30px;font-weight:900;color:#ffd166;margin:4px 0}
.btarc .grid{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin:14px 0 4px}
.btarc .lv{aspect-ratio:1;border-radius:16px;border:0;background:linear-gradient(180deg,#ff8a3d,#e04a00);color:#fff;font:900 22px Rubik,system-ui,sans-serif;display:grid;place-items:center;align-content:center;gap:2px;box-shadow:0 4px 0 #9e3300;cursor:pointer;padding:0}
.btarc .lv small{font-size:12px;letter-spacing:1px;color:#ffe066;text-shadow:0 1px 0 rgba(0,0,0,.4)}
.btarc .lv.lock{background:#2b3170;box-shadow:0 4px 0 #1a1e4d;color:#7d84b8;cursor:default}
.btarc .lv:focus-visible,.btarc .btn:focus-visible,.btarc .bar button:focus-visible{outline:3px solid #ffd166;outline-offset:2px}
@media (prefers-reduced-motion:reduce){.btarc .stars span{transition:none}}`;
function stage(title){ if(!document.getElementById('btarc-css')){ const st=document.createElement('style'); st.id='btarc-css'; st.textContent=CSS; document.head.appendChild(st); }
  const el=document.createElement('div'); el.className='btarc';
  el.innerHTML=`<canvas></canvas><div class="bar"><button class="x" aria-label="יציאה">✕</button><b></b><span class="sc">0</span><button class="snd" aria-label="צליל">🔊</button></div>`;
  el.querySelector('b').textContent=title; document.body.appendChild(el);
  const cv=el.querySelector('canvas'), cx=cv.getContext('2d'), S={el,cv,cx,w:0,h:0,dpr:1};
  S.fit=()=>{ const r=cv.getBoundingClientRect(); S.dpr=Math.min(2,window.devicePixelRatio||1); S.w=r.width; S.h=r.height; cv.width=Math.round(r.width*S.dpr); cv.height=Math.round(r.height*S.dpr); cx.setTransform(S.dpr,0,0,S.dpr,0,0); };
  S.fit(); S.score=v=>{ el.querySelector('.sc').textContent=v; };
  S.panel=html=>{ let p=el.querySelector('.panel'); if(!html){ if(p) p.remove(); return null; } if(!p){ p=document.createElement('div'); p.className='panel'; el.appendChild(p); } p.innerHTML=html; return p; };
  return S; }

// ---------- sound: a few synthesised voices, quiet, through the mall's own audio when it has one ----------
const SFX={ctx:null,on:(()=>{ try{ return localStorage.getItem('bt-arc-snd')!=='0'; }catch(e){ return true; } })()};
function ac(){ if(!SFX.ctx){ try{ SFX.ctx=(A.host&&A.host.audio)||new (window.AudioContext||window.webkitAudioContext)(); }catch(e){} } if(SFX.ctx&&SFX.ctx.state==='suspended') SFX.ctx.resume(); return SFX.ctx; }
function tone(f,d,type,vol,when,slide){ if(!SFX.on) return; const c=ac(); if(!c) return; const t=c.currentTime+(when||0), o=c.createOscillator(), g=c.createGain();
  o.type=type||'sine'; o.frequency.setValueAtTime(f,t); if(slide) o.frequency.exponentialRampToValueAtTime(Math.max(30,slide),t+d);
  g.gain.setValueAtTime(0,t); g.gain.linearRampToValueAtTime(vol||.12,t+.008); g.gain.exponentialRampToValueAtTime(.0008,t+d); o.connect(g); g.connect(c.destination); o.start(t); o.stop(t+d+.02); }
function noise(d,vol,freq,q,when){ if(!SFX.on) return; const c=ac(); if(!c) return; const t=c.currentTime+(when||0), n=Math.max(1,Math.floor(c.sampleRate*d)), b=c.createBuffer(1,n,c.sampleRate), ch=b.getChannelData(0);
  for(let i=0;i<n;i++) ch[i]=(Math.random()*2-1)*Math.pow(1-i/n,2);
  const s=c.createBufferSource(), f=c.createBiquadFilter(), g=c.createGain(); s.buffer=b; f.type='bandpass'; f.frequency.value=freq||900; f.Q.value=q||.8; g.gain.value=vol||.2; s.connect(f); f.connect(g); g.connect(c.destination); s.start(t); }
A.sfx={
  stretch(k){ tone(220+k*260,.05,'triangle',.03); },
  launch(){ noise(.25,.22,1400,.6); tone(500,.22,'sine',.06,0,180); },
  thud(k){ noise(.12,Math.min(.35,.06+k*.3),260,.9); },
  crack(mat){ const f={card:700,wood:420,glass:2600,metal:1500}[mat]||600; noise(.18,.25,f,mat==='glass'?3:1.2); if(mat==='glass') tone(2400,.18,'triangle',.05,0,3600); if(mat==='metal') tone(820,.35,'square',.04,0,760); },
  pop(){ tone(880,.08,'square',.06); tone(1320,.12,'triangle',.07,.05); noise(.08,.15,3000,1); },
  power(){ tone(660,.09,'triangle',.08); tone(990,.14,'triangle',.08,.07); },
  boom(){ noise(.6,.45,120,.5); tone(90,.5,'sine',.25,0,40); },
  merge(t){ const f=330*Math.pow(1.12246,t*2); tone(f,.12,'triangle',.09); tone(f*1.5,.16,'sine',.06,.06); },
  drop(){ tone(420,.07,'sine',.05,0,300); },
  star(i){ tone([784,988,1319][i]||1319,.3,'triangle',.11); tone(([784,988,1319][i]||1319)*2,.25,'sine',.04,.03); },
  win(){ [523,659,784,1047].forEach((f,i)=>tone(f,.22,'triangle',.1,i*.11)); },
  lose(){ [392,330,262].forEach((f,i)=>tone(f,.28,'triangle',.08,i*.16)); },
  click(){ tone(700,.04,'square',.04); } };
A.sound=v=>{ if(v!==undefined){ SFX.on=v; try{ localStorage.setItem('bt-arc-snd',v?'1':'0'); }catch(e){} } return SFX.on; };
A.buzz=p=>{ try{ if(A.host&&A.host.buzz) A.host.buzz(p); else navigator.vibrate&&navigator.vibrate(p); }catch(e){} };

// ---------- particles and words ----------
A.fx=function(){ const F={parts:[],words:[],shake:0};
  F.burst=(x,y,cols,n,speed,size,grav)=>{ for(let i=0;i<n;i++){ const a=Math.random()*Math.PI*2, v=(speed||220)*(.35+Math.random()*.65); F.parts.push({x,y,vx:Math.cos(a)*v,vy:Math.sin(a)*v-(speed||220)*.25,r:(size||4)*(.5+Math.random()),c:cols[(Math.random()*cols.length)|0],t:0,life:.45+Math.random()*.5,g:grav===undefined?700:grav,rot:Math.random()*6,vr:(Math.random()-.5)*14,sq:Math.random()<.5}); } };
  F.word=(x,y,text,col,size,life,tag)=>{ if(tag) F.words=F.words.filter(w=>w.tag!==tag); if(F.words.length>6) F.words.shift(); F.words.push({x,y,text,col,size:size||28,t:0,life:life||1,tag,rtl:/[\u0590-\u05FF]/.test(text)}); };
  F.step=dt=>{ F.shake=Math.max(0,F.shake-dt*2.2);
    for(let i=F.parts.length-1;i>=0;i--){ const p=F.parts[i]; p.t+=dt; if(p.t>p.life){ F.parts.splice(i,1); continue; } p.vy+=p.g*dt; p.x+=p.vx*dt; p.y+=p.vy*dt; p.rot+=p.vr*dt; }
    for(let i=F.words.length-1;i>=0;i--){ const w=F.words[i]; w.t+=dt; if(w.t>w.life) F.words.splice(i,1); } };
  F.draw=(g,k)=>{ k=k||1; F.parts.forEach(p=>{ g.globalAlpha=Math.max(0,1-p.t/p.life); g.fillStyle=p.c; if(p.sq){ g.save(); g.translate(p.x,p.y); g.rotate(p.rot); g.fillRect(-p.r,-p.r*.6,p.r*2,p.r*1.2); g.restore(); } else { g.beginPath(); g.arc(p.x,p.y,p.r,0,7); g.fill(); } });
    g.globalAlpha=1; g.textAlign='center'; g.textBaseline='middle';
    F.words.forEach(w=>{ const u=w.t/w.life, s=w.size*k*(u<.15?.6+u/.15*.5:1.1-u*.1); g.globalAlpha=u>.7?1-(u-.7)/.3:1; g.font=`900 ${s}px Rubik,system-ui,sans-serif`; g.direction=w.rtl?'rtl':'ltr';
      g.lineWidth=s*.16; g.lineJoin='round'; g.strokeStyle='rgba(12,14,40,.85)'; g.strokeText(w.text,w.x,w.y-u*40*k); g.fillStyle=w.col; g.fillText(w.text,w.x,w.y-u*40*k); });
    g.globalAlpha=1; };
  return F; };

// ---------- saved progress ----------
A.load=(k,d)=>{ try{ const v=JSON.parse(localStorage.getItem('bt-arc-'+k)); return v==null?d:v; }catch(e){ return d; } };
A.save=(k,v)=>{ try{ localStorage.setItem('bt-arc-'+k,JSON.stringify(v)); }catch(e){} };
A.rr=(g,x,y,w,h,r)=>{ g.beginPath(); g.moveTo(x+r,y); g.arcTo(x+w,y,x+w,y+h,r); g.arcTo(x+w,y+h,x,y+h,r); g.arcTo(x,y+h,x,y,r); g.arcTo(x,y,x+w,y,r); g.closePath(); };
A.img=src=>{ const i=new Image(); i.crossOrigin='anonymous'; i.src=src; return i; };
// an image, loaded (resolves even on failure, so a missing picture never blocks a game)
A.pic=src=>new Promise(res=>{ const i=new Image(); i.onload=()=>res(i); i.onerror=()=>res(null); i.src=src; });
// a sprite sheet: art/<name>.webp with art/<name>.json {key:[x,y,w,h]}
A.atlas=async name=>{ const [img,map]=await Promise.all([A.pic(base+'art/'+name+'.webp?v='+(A.v||1)),fetch(base+'art/'+name+'.json?v='+(A.v||1)).then(r=>r.json()).catch(()=>({}))]);
  const T={img,map,has:k=>!!(img&&map[k]),
    // the sprite centred on x,y, w wide (h follows its shape unless given)
    draw(g,k,x,y,w,h){ const r=map[k]; if(!img||!r) return; h=h||w*r[3]/r[2]; g.drawImage(img,r[0],r[1],r[2],r[3],x-w/2,y-h/2,w,h); },
    // stretched by its middle: the corners keep their shape (c = corner in the sheet, d = on screen)
    nine(g,k,x,y,w,h,c,d){ const r=map[k]; if(!img||!r) return; const [sx,sy,sw,sh]=r; d=Math.min(d,w/2,h/2);
      const xs=[sx,sx+c,sx+sw-c,sx+sw], ys=[sy,sy+c,sy+sh-c,sy+sh], X=[x,x+d,x+w-d,x+w], Y=[y,y+d,y+h-d,y+h];
      for(let j=0;j<3;j++) for(let i=0;i<3;i++){ const dw=X[i+1]-X[i], dh=Y[j+1]-Y[j]; if(dw>.01&&dh>.01) g.drawImage(img,xs[i],ys[j],xs[i+1]-xs[i],ys[j+1]-ys[j],X[i]-.25,Y[j]-.25,dw+.5,dh+.5); } } };
  return T; };

// ---------- open and close ----------
let CUR=null;
A.open=async function(id,host){
  if(CUR) A.close(); A.host=host||{};
  const [M,G]=await Promise.all([A.matter(),gameScript(id)]);
  const S=stage(G.title); CUR={S,game:null};
  if(G.load){ S.panel('<div class="card"><h2>'+G.title+'</h2><p>טוען…</p></div>'); const st=CUR; await G.load(); if(CUR!==st) return null; S.panel(null); }
  const close=()=>A.close();
  S.el.querySelector('.x').onclick=()=>{ A.sfx.click(); close(); };
  const snd=S.el.querySelector('.snd'); snd.textContent=A.sound()?'🔊':'🔇'; snd.onclick=()=>{ A.sound(!A.sound()); snd.textContent=A.sound()?'🔊':'🔇'; };
  const onResize=()=>{ S.fit(); CUR&&CUR.game&&CUR.game.resize&&CUR.game.resize(); }; addEventListener('resize',onResize); CUR.off=()=>removeEventListener('resize',onResize);
  CUR.game=G.start(S,M,A.host); return CUR.game; };
A.close=function(){ if(!CUR) return; try{ CUR.game&&CUR.game.stop&&CUR.game.stop(); }catch(e){} CUR.off&&CUR.off(); CUR.S.el.remove(); const h=A.host; CUR=null; if(h&&h.closed) h.closed(); };
A.isOpen=()=>!!CUR;
})();
