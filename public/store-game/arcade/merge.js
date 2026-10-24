/* merge.js: 'מיזוג הדילים', the mall's take on the watermelon game. Products drop into a glass box; two of
   the same touch and become the next, dearer one, up an eleven-step ladder of real products from the catalogue
   (cheapest first) to the jackpot. The box fills; a product resting above the red line for two seconds ends the
   round. Merges that follow one another make a chain, which counts double, triple. */
(function(){
const A=window.BTArcade;
const BW=400, BH=560, LINE=78;                                    // the box, in world units; the danger line from its top
const R=[17,22,28,34,41,49,58,68,80,94,110];                       // each step's radius
const COL=['#ffd43b','#74c0fc','#ffe8cc','#ff8fa3','#5c7cfa','#ff6b6b','#f783ac','#66d9e8','#adb5bd','#9775fa','#fab005'];
const DROP=[.3,.28,.2,.14,.08];                                    // what can be dropped: the first five steps
// one step: a glossy bubble in its colour with the item inside (the bubble is what the physics feels)
function paintItem(g,ART,t,r,a){ const c=COL[t];
  const gr=g.createRadialGradient(-r*.3,-r*.35,r*.1,0,0,r); gr.addColorStop(0,'#fff'); gr.addColorStop(.35,c); gr.addColorStop(1,shade(c,.78));
  g.fillStyle=gr; g.beginPath(); g.arc(0,0,r,0,7); g.fill();
  g.lineWidth=Math.max(1.2,r*.07); g.strokeStyle=shade(c,.55); g.stroke();
  const m=ART&&ART.map['i'+t]; if(m){ const s=r*1.6/Math.max(m[2],m[3]); ART.draw(g,'i'+t,0,r*.03,m[2]*s,m[3]*s); }
  g.fillStyle='rgba(255,255,255,.55)'; g.beginPath(); g.ellipse(-r*.38,-r*.5,r*.32,r*.15,-.6,0,7); g.fill(); }
function shade(hex,k){ const n=parseInt(hex.slice(1),16); return 'rgb('+[(n>>16)&255,(n>>8)&255,n&255].map(v=>Math.round(v*k)).join(',')+')'; }
const NAME=['מטבע','סוכרייה','קפה','שפתון','אוזניות','סניקרס','תיק','שעון','מצלמה','סמארטפון','מתנת ג׳קפוט'];
A.games.merge={title:'מיזוג הדילים',
  async load(){ const G=A.games.merge; if(G.art) return; [G.art,G.bg]=await Promise.all([A.atlas('merge'),A.pic(A.base+'art/bg-merge.jpg?v='+(A.v||1))]);
    // small pictures of each step, for the cards
    G.icons=R.map((_,t)=>{ try{ const c=document.createElement('canvas'); c.width=c.height=96; const x=c.getContext('2d'); x.translate(48,48); paintItem(x,G.art,t,44,1); return c.toDataURL(); }catch(e){ return ''; } }); },
  start(S,M,host){
  const {Engine,Bodies,Composite,Events,Body}=M, g=S.cx, F=A.fx(), ART=A.games.merge.art, BG=A.games.merge.bg, ICON=A.games.merge.icons||[];
  let E, box, view={k:1,ox:0,oy:0}, st, raf=0, last=0, acc=0;
  const best=()=>A.load('merge-best',0), bestTier=()=>A.load('merge-tier',0);
  function fit(){ const top=64+72, sidePad=8, bottom=10; const k=Math.min((S.w-sidePad*2)/(BW+72),(S.h-top-bottom)/(BH+110)); view.k=k; view.ox=(S.w-BW*k)/2; view.oy=top+((S.h-top-bottom)-(BH+110)*k)/2+70*k; }
  function newRound(){
    E=Engine.create({gravity:{x:0,y:1.15}}); E.positionIterations=8; E.velocityIterations=6;
    const wall={isStatic:true,friction:.05,restitution:.1,label:'wall'};
    Composite.add(E.world,[Bodies.rectangle(BW/2,BH+40,BW+200,80,wall),Bodies.rectangle(-40,BH/2-200,80,BH+600,wall),Bodies.rectangle(BW+40,BH/2-200,80,BH+600,wall)]);
    st={score:0,chain:0,chainT:0,danger:0,over:false,cur:pickTier(),next:pickTier(),x:BW/2,cool:0,merges:[],t:0,top:bestTier(),newTop:false,flash:0};
    Events.on(E,'collisionStart',onHit); Events.on(E,'collisionActive',onHit);
    S.score(0); }
  function pickTier(){ let r=Math.random(), i=0; while(i<DROP.length-1&&r>DROP[i]){ r-=DROP[i]; i++; } return i; }
  function onHit(ev){ ev.pairs.forEach(p=>{ const a=p.bodyA, b=p.bodyB; if(a.tier===undefined||b.tier===undefined||a.tier!==b.tier||a.gone||b.gone) return; a.gone=b.gone=true; st.merges.push([a,b]); }); }
  function addItem(t,x,y,vx,vy,born){ const r=R[t], b=Bodies.circle(x,y,r,{restitution:.12,friction:.06,frictionStatic:.25,frictionAir:.004,density:.0011,label:'item'});
    b.tier=t; b.born=born===undefined?st.t:born; b.pop=0; Body.setVelocity(b,{x:vx||0,y:vy||0}); Composite.add(E.world,b); return b; }
  function drop(){ if(st.over||st.cool>0) return; const t=st.cur, r=R[t], x=Math.max(r+2,Math.min(BW-r-2,st.x));
    addItem(t,x,-r-6,0,2); st.cur=st.next; st.next=pickTier(); st.cool=.55; A.sfx.drop(); A.buzz(8); }
  function doMerges(){ st.merges.forEach(([a,b])=>{ const t=a.tier, x=(a.position.x+b.position.x)/2, y=(a.position.y+b.position.y)/2;
      Composite.remove(E.world,a); Composite.remove(E.world,b);
      st.chain=st.chainT>0?st.chain+1:1; st.chainT=.9; const mult=Math.min(4,st.chain);
      if(t>=R.length-1){ // two jackpots: they go off together
        const pts=500*mult; st.score+=pts; F.burst(x,y,['#ffd166','#fff','#ff7a2e','#fab005'],70,620,7); F.word(x,y-40,'🎉 ג׳קפוט! +'+pts,'#ffd166',40,1.6); F.shake=1; A.sfx.boom(); A.buzz([40,60,40]); return; }
      const n=t+1, nb=addItem(n,x,y,(a.velocity.x+b.velocity.x)/2,Math.min(0,(a.velocity.y+b.velocity.y)/2)-1.5); nb.pop=1;
      const pts=((n+1)*(n+2)/2)*mult; st.score+=pts;
      F.burst(x,y,[COL[n],'#fff',COL[t]],10+n*3,160+n*30,3+n*.3); F.word(x,y-R[n]-10,'+'+pts+(mult>1?' ×'+mult:''),mult>1?'#ffd166':'#fff',20+n*2,.9);
      if(st.chain>=3) F.word(BW/2,LINE+40,'שרשרת ×'+st.chain+'!','#ff7a2e',34,1.1,'chain');
      if(n>st.top){ st.top=n; st.newTop=true; A.save('merge-tier',n); F.word(x,y-R[n]-48,'חדש! '+NAME[n]+' ('+(n+1)+'/11)','#7bff9b',26,1.6); A.sfx.star(2); }
      F.shake=Math.min(1,.15+n*.07); A.sfx.merge(n); A.buzz(n>5?[20,30,20]:12); });
    st.merges.length=0; }
  function step(dt){ st.t+=dt; st.cool=Math.max(0,st.cool-dt); st.chainT=Math.max(0,st.chainT-dt); if(st.chainT<=0) st.chain=0;
    acc+=Math.min(dt,.05); let n=0; while(acc>=1/60&&n<3){ Engine.update(E,1000/60); doMerges(); acc-=1/60; n++; }
    // the danger line: anything that has come to rest above it, for long enough, ends the round
    let over=false; Composite.allBodies(E.world).forEach(b=>{ if(b.tier===undefined) return; b.pop=Math.max(0,b.pop-dt*5);
      if(st.t-b.born>1.4&&b.position.y-R[b.tier]<LINE&&b.speed<1.2) over=true; });
    st.danger=over?st.danger+dt:Math.max(0,st.danger-dt*2); if(st.danger>2.2&&!st.over) gameOver();
    F.step(dt); S.score(st.score); }
  function gameOver(){ st.over=true; A.sfx.lose(); A.buzz([60,80,60]);
    const b=best(), nb=st.score>b; if(nb) A.save('merge-best',st.score); const coins=Math.max(1,Math.floor(st.score/30)); host.coins&&host.coins(coins);
    setTimeout(()=>{ const p=S.panel(`<div class="card"><h2>${nb?'שיא חדש! 🏆':'הקופסה התמלאה'}</h2><p style="font-size:44px;font-weight:900;color:#fff;margin:6px 0">${st.score}</p>
      <p>השיא שלכם: <b>${Math.max(b,st.score)}</b> · הכי רחוק שהגעתם: <b>${NAME[st.top]} (${st.top+1}/11)</b></p><div class="coins">+${coins} 🪙</div>
      <div class="row"><button class="btn again">עוד סיבוב</button><button class="btn ghost out">חזרה לקניון</button></div></div>`);
      p.querySelector('.again').onclick=()=>{ A.sfx.click(); S.panel(null); newRound(); }; p.querySelector('.out').onclick=()=>A.close(); },900); }
  // ---------- drawing: the shop behind, a glass case on a wooden base, each step a glossy bubble with its item ----------
  const drawItem=(t,x,y,s,alpha)=>{ g.save(); g.globalAlpha=alpha===undefined?1:alpha; g.translate(x,y); paintItem(g,ART,t,R[t]*s,1); g.restore(); };
  function draw(){ const W=S.w, H=S.h, k=view.k;
    // the shop, filling the screen
    if(BG){ const s=Math.max(W/BG.width,H/BG.height); g.drawImage(BG,(W-BG.width*s)/2,(H-BG.height*s)/2,BG.width*s,BG.height*s); } else { g.fillStyle='#f6e7d2'; g.fillRect(0,0,W,H); }
    const top=g.createLinearGradient(0,0,0,170); top.addColorStop(0,'rgba(40,24,10,.55)'); top.addColorStop(1,'rgba(40,24,10,0)'); g.fillStyle=top; g.fillRect(0,0,W,170);
    drawStrip(W);
    const sh=F.shake>0?(Math.random()-.5)*F.shake*10:0;
    g.save(); g.translate(view.ox+sh,view.oy); g.scale(k,k);
    // the case: its glass back, then (after the items) the glass walls and the wooden base in front
    g.fillStyle='rgba(255,255,255,.42)'; A.rr(g,-4,-30,BW+8,BH+34,16); g.fill();
    const in_=g.createLinearGradient(0,0,0,BH); in_.addColorStop(0,'rgba(255,255,255,.25)'); in_.addColorStop(1,'rgba(255,236,210,.35)'); g.fillStyle=in_; g.fillRect(0,0,BW,BH);
    const warn=st.danger>0; g.strokeStyle=warn?`rgba(235,40,40,${.55+.45*Math.sin(st.t*14)})`:'rgba(214,60,40,.45)'; g.lineWidth=4; g.setLineDash([16,10]); g.beginPath(); g.moveTo(4,LINE); g.lineTo(BW-4,LINE); g.stroke(); g.setLineDash([]);
    // the one about to drop, and where it will fall
    if(!st.over){ const r=R[st.cur], x=Math.max(r+2,Math.min(BW-r-2,st.x));
      g.strokeStyle='rgba(120,70,30,.35)'; g.lineWidth=3; g.setLineDash([3,9]); g.lineCap='round'; g.beginPath(); g.moveTo(x,0); g.lineTo(x,BH); g.stroke(); g.setLineDash([]);
      drawItem(st.cur,x,-r-8+Math.sin(st.t*3)*2,1,st.cool>0?.45:1); }
    Composite.allBodies(E.world).forEach(b=>{ if(b.tier===undefined) return; g.save(); g.translate(b.position.x,b.position.y); g.rotate(b.angle); const s=1+b.pop*.22; g.scale(s,s); paintItem(g,ART,b.tier,R[b.tier],1); g.restore(); });
    F.draw(g,1);
    g.restore();
    g.save(); g.translate(view.ox+sh,view.oy); g.scale(k,k);
    [[-20],[BW]].forEach(([x])=>{ g.save(); g.translate(x+10,BH/2-12); g.rotate(Math.PI/2); ART.nine(g,'glass',-(BH+46)/2,-11,BH+46,22,15,8); g.restore(); });
    ART.nine(g,'wood',-34,BH,BW+68,36,15,12);
    g.restore();
    if(st.danger>0&&!st.over){ g.fillStyle=`rgba(255,40,60,${Math.min(.25,st.danger*.12)})`; g.fillRect(0,0,W,H); } }
  function drawStrip(W){ const y=64+34, n=R.length, w=Math.min(W-110,520), x0=(W-w)/2+44;
    g.textAlign='center'; g.textBaseline='middle'; g.direction='rtl';
    // next
    const nx=(W-w)/2-28; g.fillStyle='rgba(255,255,255,.88)'; A.rr(g,nx-28,y-30,56,60,16); g.fill(); g.font='800 10px Rubik,sans-serif'; g.fillStyle='#8a4b1c'; g.fillText('הבא',nx,y-20);
    drawItem(st.next,nx,y+7,17/R[st.next]);
    // the ladder: what has been reached, lit; the next to find, outlined
    g.fillStyle='rgba(255,255,255,.55)'; A.rr(g,x0-16,y-19,w-8,38,19); g.fill();
    for(let i=0;i<n;i++){ const x=x0+i*(w-40)/(n-1), lit=i<=st.top, r=8+i*.75;
      if(lit) drawItem(i,x,y,r/R[i]);
      else { g.save(); g.globalAlpha=.28; g.filter='grayscale(1)'; drawItem(i,x,y,r/R[i]); g.restore(); if(i===st.top+1){ g.strokeStyle='#ff7a2e'; g.lineWidth=2.5; g.beginPath(); g.arc(x,y,r+2,0,7); g.stroke(); } } } }
  // ---------- input ----------
  const pos=e=>{ const r=S.cv.getBoundingClientRect(); return (e.clientX-r.left-view.ox)/view.k; };
  let down=false;
  const onDown=e=>{ if(st.over||S.el.querySelector('.panel')) return; down=true; st.x=pos(e); try{ S.cv.setPointerCapture(e.pointerId); }catch(_){} };
  const onMove=e=>{ if(st.over) return; if(down||e.pointerType==='mouse') st.x=pos(e); };
  const onUp=e=>{ if(!down) return; down=false; st.x=pos(e); drop(); };
  const onKey=e=>{ if(st.over||S.el.querySelector('.panel')) return; if(e.code==='ArrowLeft') st.x-=24; else if(e.code==='ArrowRight') st.x+=24; else if(e.code==='Space'||e.code==='Enter'){ e.preventDefault(); drop(); } st.x=Math.max(0,Math.min(BW,st.x)); };
  S.cv.addEventListener('pointerdown',onDown); S.cv.addEventListener('pointermove',onMove); S.cv.addEventListener('pointerup',onUp); addEventListener('keydown',onKey);
  function loop(now){ const dt=Math.min(.05,(now-last)/1000||0); last=now; if(!S.el.querySelector('.panel')||st.over) step(dt); draw(); raf=requestAnimationFrame(loop); }
  // ---------- start ----------
  fit(); newRound();
  const firsts=[0,1,2,3,4].map(i=>ICON[i]?`<img src="${ICON[i]}" alt="${NAME[i]}" style="width:${26+i*4}px;height:${26+i*4}px">`:'').join('<span style="opacity:.6">→</span>')+(ICON[10]?`<span style="opacity:.6">…</span><img src="${ICON[10]}" alt="${NAME[10]}" style="width:48px;height:48px">`:'');
  const p=S.panel(`<div class="card"><h2>🧩 מיזוג הדילים</h2><p>זורקים מוצרים לוויטרינה. <b>שני מוצרים זהים שנוגעים מתאחדים</b> למוצר הבא, היקר יותר, עד הג׳קפוט.</p>
    <div style="display:flex;flex-wrap:wrap;align-items:center;justify-content:center;gap:3px;margin:10px 0;direction:ltr">${firsts}</div>
    <p>גוררים לכוון, משחררים לזרוק. מיזוגים ברצף = <b>שרשרת</b> ונקודות כפולות. אם מוצר נשאר מעל הקו האדום, נגמר.</p>
    <p>השיא שלכם: <b>${best()}</b></p><div class="row"><button class="btn go">יאללה!</button></div></div>`);
  p.querySelector('.go').onclick=()=>{ A.sfx.click(); S.panel(null); };
  last=performance.now(); raf=requestAnimationFrame(loop);
  return {resize(){ fit(); }, stop(){ cancelAnimationFrame(raf); removeEventListener('keydown',onKey); },
    debug:{st:()=>st,E:()=>E,addItem:(t,x,y)=>addItem(t,x,y),drop,setX:x=>{ st.x=x; }}};
}};
})();
