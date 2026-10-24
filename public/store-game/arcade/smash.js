/* smash.js: 'מבצע הריסה', the mall's slingshot game. Grumpy full-price tags hide in towers of cardboard,
   wood, glass and metal; a slingshot shoots shopping bags at them. Every tag gone, the level is won, with up to
   three stars for the score (and a bonus for every bag not needed). Five kinds of bag, met one by one:
     the bag      a plain shot
     the trio     tap in flight: three coupons
     the flash    tap in flight: a burst of speed straight on
     the safe     heavy: goes through what the others bounce off
     the bomb     tap in flight, or on hitting something: it goes off
   The physics (core) is kept apart from the drawing, so the levels can be played by a script with no screen
   (to prove each one stands up by itself and can be won). */
(function(){
const A=window.BTArcade;
const MAT={card:{d:.0007,hp:38,col:'#d8a467',dark:'#a97a43',pts:300},wood:{d:.0009,hp:90,col:'#bd7a3e',dark:'#7c4a1e',pts:500},
  glass:{d:.0011,hp:26,col:'rgba(175,225,255,.62)',dark:'#7fb8dd',pts:400},metal:{d:.0024,hp:260,col:'#a3acbd',dark:'#5d6576',pts:800},
  tnt:{d:.0010,hp:22,col:'#ffcf3d',dark:'#c99400',pts:600}};
const BAG={bag:{r:22,d:.004,col:'#e63946',name:'שקית'},trio:{r:20,d:.004,col:'#3a86ff',name:'שלישייה'},flash:{r:20,d:.004,col:'#ffbe0b',name:'בזק'},
  safe:{r:26,d:.011,col:'#2b2d42',name:'כספת'},bomb:{r:23,d:.005,col:'#ff7a2e',name:'פצצה'}};
const SLING={x:190,y:-178}, PULL=120, VMAX=21, G=1;

// ---------- the levels: blocks [material, x, y(centre, up is negative), w, h, angle?], tags [x, y, boss?] ----------
// helpers that place by the ground (y=0 is the floor): a column, a beam over two columns, a box
function L(){ const b=[], t=[]; const o={b,t,
  col(m,x,base,h,w){ w=w||20; b.push([m,x,base-h/2,w,h]); return base-h; },
  beam(m,x0,x1,y,h){ h=h||20; b.push([m,(x0+x1)/2,y-h/2,x1-x0,h]); return y-h; },
  box(m,x,base,s,h){ h=h||s; b.push([m,x,base-h/2,s,h]); return base-h; },
  // a frame: two columns and a beam on them, standing on `base`; returns the top
  frame(m,x0,x1,base,h,beamM){ o.col(m,x0+10,base,h); o.col(m,x1-10,base,h); return o.beam(beamM||m,x0,x1,base-h); },
  tag(x,base,boss){ const r=boss?32:24; t.push([x,base-r-1,!!boss]); return base-2*r; } }; return o; }
const LEVELS=[
  // 1: one tag on a stack of boxes
  (()=>{ const l=L(); let y=l.box('card',1000,0,70); y=l.box('card',1000,y,70); l.tag(1000,y); return {bags:['bag','bag','bag'],...l}; })(),
  // 2: a wooden hut with a tag inside and one on top
  (()=>{ const l=L(); const y=l.frame('wood',930,1090,0,110); l.tag(1010,0); l.tag(1010,y); return {bags:['bag','bag','bag'],...l}; })(),
  // 3: the trio: three glass cases in a row, a crate of TNT on the middle one
  (()=>{ const l=L(); [900,1040,1180].forEach(x=>{ const y=l.frame('glass',x-55,x+55,0,90); l.tag(x,0); l.box(x===1040?'tnt':'card',x,y,x===1040?44:50); }); return {bags:['trio','trio','bag'],...l}; })(),
  // 4: a two-storey wooden fort with TNT on its ground floor
  (()=>{ const l=L(); let y=l.frame('wood',900,1120,0,120); l.tag(955,0); l.tag(1065,0); l.box('tnt',1010,0,40); y=l.frame('wood',930,1090,y,100); l.tag(1010,y+100); l.box('card',1010,y,60); l.tag(1010,y-60); return {bags:['bag','trio','bag','trio'],...l}; })(),
  // 5: the flash: a tag behind a tall wooden wall
  (()=>{ const l=L(); l.col('wood',920,0,220,24); l.col('wood',950,0,220,24); l.tag(1060,0); const y=l.frame('card',1000,1120,0,80); l.tag(1060,y); return {bags:['flash','flash','bag'],...l}; })(),
  // 6: the safe: metal and glass
  (()=>{ const l=L(); let y=l.frame('metal',900,1060,0,100,'glass'); l.tag(980,0); y=l.frame('glass',920,1040,y,90); l.tag(980,y+90); l.box('wood',1150,0,80); l.tag(1150,-80); return {bags:['safe','bag','safe'],...l}; })(),
  // 7: two towers and a bridge
  (()=>{ const l=L(); let a=l.frame('wood',860,960,0,140); let c=l.frame('wood',1140,1240,0,140); l.beam('wood',940,1160,a,20); l.tag(1050,a-20); l.tag(910,0); l.tag(1190,0); l.box('card',910,a,50); l.box('card',1190,c,50); return {bags:['trio','flash','bag','safe'],...l}; })(),
  // 8: the bomb: a metal box full of tags
  (()=>{ const l=L(); l.col('metal',900,0,130); l.col('metal',1140,0,130); l.beam('metal',890,1150,-130); l.tag(970,0); l.tag(1030,0); l.tag(1090,0,true); return {bags:['bomb','bomb','safe'],...l}; })(),
  // 9: a pyramid of boxes
  (()=>{ const l=L(); const s=60; [[0,5],[1,4],[2,3],[3,2],[4,1]].forEach(([row,n])=>{ for(let i=0;i<n;i++){ const x=1000+(i-(n-1)/2)*s; l.box(row%2?'wood':'card',x,-row*s,s); } }); l.tag(1000,-5*s); l.tag(1220,0); return {bags:['bag','trio','bomb'],...l}; })(),
  // 10: the boss in a glass tower
  (()=>{ const l=L(); let y=0; for(let k=0;k<3;k++){ y=l.frame('glass',950,1090,y,100,k===2?'metal':'glass'); } l.tag(1020,0,true); l.tag(1020,-120); l.tag(1020,y); return {bags:['flash','safe','trio','bag'],...l}; })(),
  // 11: a wall of crates on a ledge
  (()=>{ const l=L(); l.frame('metal',880,1260,0,120); for(let i=0;i<5;i++) l.box(i%2?'wood':'card',920+i*76,-140,70); l.tag(960,0); l.tag(1180,0); l.tag(1070,-210,true); return {bags:['bomb','trio','safe','bag'],...l}; })(),
  // 12: the castle
  (()=>{ const l=L(); let y=l.frame('metal',880,1000,0,120); l.frame('metal',1140,1260,0,120); l.beam('wood',990,1150,-140); l.tag(940,0); l.tag(1200,0); l.tag(1070,0,true);
    l.frame('wood',900,980,y,80); l.tag(940,y); l.frame('wood',1160,1240,y,80); l.tag(1200,y); l.box('glass',1070,-160,60,100); return {bags:['safe','bomb','trio','flash','bag'],...l}; })(),
  // 13: stilts
  (()=>{ const l=L(); [920,1040,1160].forEach((x,i)=>{ l.col('wood',x-30,0,200,16); l.col('wood',x+30,0,200,16); const y=l.beam('glass',x-48,x+48,-200); l.tag(x,y,i===1); }); return {bags:['flash','trio','bomb','bag'],...l}; })(),
  // 14: the vault
  (()=>{ const l=L(); l.col('metal',880,0,180,30); l.col('metal',1200,0,180,30); l.beam('metal',860,1220,-180,30); l.frame('glass',930,1150,0,90,'wood'); l.tag(990,0); l.tag(1090,0); l.tag(1040,-130,true); l.box('card',1310,0,70); l.tag(1310,-70); return {bags:['safe','bomb','bomb','trio'],...l}; })(),
  // 15: the grand sale tower
  (()=>{ const l=L(); let y=0; const mats=['metal','wood','glass','wood','card']; mats.forEach((m,k)=>{ y=l.frame(m,960-k*6,1140+k*6,y,90); l.tag(1050,y+90,k===0||k===4); }); l.box('wood',1260,0,80); l.tag(1260,-80); return {bags:['bomb','safe','trio','flash','bag'],...l}; })(),
];

// ---------- the core: one level's world ----------
function core(M,def){
  const {Engine,Bodies,Composite,Events,Body,Vector}=M;
  const E=Engine.create({gravity:{x:0,y:G}, enableSleeping:false}); E.positionIterations=10; E.velocityIterations=8;
  const W={E,M,blocks:[],tags:[],shots:[],score:0,t:0,quiet:0,events:[],settling:1.4};
  Composite.add(E.world,Bodies.rectangle(800,100,6000,200,{isStatic:true,friction:.9,label:'ground'}));
  def.b.forEach(([m,x,y,w,h,a])=>{ const P=MAT[m], b=Bodies.rectangle(x,y,w,h,{density:P.d,friction:.75,frictionStatic:1.2,restitution:.04,angle:a||0,label:'block',slop:.02});
    b.mat=m; b.hp=P.hp; b.max=P.hp; b.w=w; b.h=h; W.blocks.push(b); Composite.add(E.world,b); });
  def.t.forEach(([x,y,boss])=>{ const r=boss?32:24, b=Bodies.circle(x,y,r,{density:.0011,friction:.6,restitution:.1,label:'tag'}); b.boss=boss; b.hp=boss?32:12; b.max=b.hp; b.r=r; W.tags.push(b); Composite.add(E.world,b); });
  const massOf=b=>b.isStatic?5:Math.min(b.mass,14);
  function hurt(b,dmg,src){ if(b.dead||dmg<=0) return; b.hp-=dmg; b.hitT=W.t; if(b.hp<=0) kill(b,src); }
  function kill(b,src){ if(b.dead) return; b.dead=true; Composite.remove(E.world,b);
    if(b.label==='tag'){ const p=b.boss?10000:5000; W.score+=p; W.events.push({k:'tag',x:b.position.x,y:b.position.y,boss:b.boss,r:b.r,a:b.angle,pts:p}); }
    else { const p=MAT[b.mat].pts; W.score+=p; W.events.push({k:'break',x:b.position.x,y:b.position.y,mat:b.mat,w:b.w,h:b.h,a:b.angle,pts:p}); if(b.mat==='tnt') blast(b.position,150,b); } }
  // an explosion: everything near is thrown out and hurt (the bomb bag, and a crate of TNT when it breaks)
  function blast(c,R,src){ c={x:c.x,y:c.y}; W.events.push({k:'boom',x:c.x,y:c.y,r:R});
    Composite.allBodies(E.world).forEach(b=>{ if(b.isStatic||b===src||b.dead) return; const d=Vector.sub(b.position,c), L2=Math.hypot(d.x,d.y); if(L2>R) return; const f=1-L2/R;
      Body.setVelocity(b,{x:b.velocity.x+d.x/(L2||1)*f*16,y:b.velocity.y+d.y/(L2||1)*f*16-f*4});
      if(b.label==='block') hurt(b,f*(b.mat==='metal'?140:120),src); else if(b.label==='tag') hurt(b,f*40,src); }); }
  Events.on(E,'collisionStart',ev=>{ if(W.t<W.settling) return;
    ev.pairs.forEach(p=>{ const a=p.bodyA, b=p.bodyB, n=p.collision.normal;
      const rv=Math.abs((a.velocity.x-b.velocity.x)*n.x+(a.velocity.y-b.velocity.y)*n.y); if(rv<1.3) return;
      const sound=rv*Math.min(massOf(a),massOf(b)); if(sound>2) W.events.push({k:'thud',x:(a.position.x+b.position.x)/2,y:(a.position.y+b.position.y)/2,v:Math.min(1,sound/40)});
      [[a,b],[b,a]].forEach(([me,other])=>{ if(me.isStatic||me.dead) return; if(me.label!=='block'&&me.label!=='tag') return;
        let k=other.label==='shot'?(other.kind==='safe'?1.9:1.15):.8; if(me.label==='tag') k*=1.3;
        hurt(me,(rv-1.2)*massOf(other)*k,other); });
      [a,b].forEach(s=>{ if(s.label==='shot'&&s.kind==='bomb'&&!s.used&&rv>3) W.ability(s); });
    }); });
  W.launch=(kind,vx,vy)=>{ const B=BAG[kind], s=Bodies.circle(SLING.x,SLING.y,B.r,{density:B.d,friction:.5,restitution:.25,frictionAir:.0005,label:'shot'}); s.kind=kind; s.born=W.t;
    Body.setVelocity(s,{x:vx,y:vy}); Composite.add(E.world,s); W.shots.push(s); W.quiet=0; W.flying=s; W.events.push({k:'launch'}); return s; };
  // what a tap does in flight
  W.ability=s=>{ if(!s||s.used||s.dead) return false; s.used=true;
    if(s.kind==='trio'){ [-.22,.22].forEach(da=>{ const v=Vector.rotate(s.velocity,da), c=Bodies.circle(s.position.x,s.position.y,BAG.trio.r*.8,{density:BAG.trio.d,friction:.5,restitution:.25,frictionAir:.0005,label:'shot'}); c.kind='trio'; c.used=true; c.born=s.born; Body.setVelocity(c,v); Composite.add(E.world,c); W.shots.push(c); }); W.events.push({k:'power',x:s.position.x,y:s.position.y,kind:'trio'}); return true; }
    if(s.kind==='flash'){ const v=s.velocity, sp=Math.hypot(v.x,v.y)||1; Body.setVelocity(s,{x:v.x/sp*24,y:v.y/sp*24}); W.events.push({k:'power',x:s.position.x,y:s.position.y,kind:'flash'}); return true; }
    if(s.kind==='bomb'){ s.dead=true; Composite.remove(E.world,s); blast(s.position,170,s); return true; }
    if(s.kind==='safe'){ Body.setVelocity(s,{x:s.velocity.x*.3,y:Math.max(14,s.velocity.y+12)}); W.events.push({k:'power',x:s.position.x,y:s.position.y,kind:'safe'}); return true; }
    return false; };
  W.step=()=>{ Engine.update(E,1000/60); W.t+=1/60;
    // out of the level: gone
    W.shots.forEach(s=>{ if(!s.dead&&(s.position.x>2600||s.position.x<-400||s.position.y>400)){ s.dead=true; Composite.remove(E.world,s); } });
    W.tags.forEach(t=>{ if(!t.dead&&(t.position.y>300||t.position.x>2600||t.position.x<-300)) kill(t); });
    let moving=0; Composite.allBodies(E.world).forEach(b=>{ if(!b.isStatic&&b.speed>.35) moving++; });
    W.quiet=moving?0:W.quiet+1/60; };
  W.alive=()=>W.tags.filter(t=>!t.dead).length;
  // a shot is over when all is still for a moment, or after nine seconds
  W.shotDone=()=>W.flying&&(W.quiet>.6&&W.t-W.flying.born>1.2||W.t-W.flying.born>9);
  W.vel=(dx,dy)=>{ const L2=Math.min(PULL,Math.hypot(dx,dy)), k=L2/PULL*VMAX/(Math.hypot(dx,dy)||1); return {x:-dx*k,y:-dy*k}; };
  return W; }

// what each level can score at most, for its stars: every tag, every block, and all bags but one left over
function potential(def){ return def.t.reduce((s,t)=>s+(t[2]?10000:5000),0)+def.b.reduce((s,b)=>s+MAT[b[0]].pts,0)*.55+(def.bags.length-1)*10000; }
function starsFor(def,score,won){ if(!won) return 0; const P=potential(def); return score>=P*.75?3:score>=P*.5?2:1; }

A.games.smash={title:'מבצע הריסה',LEVELS,core,BAG,MAT,SLING,potential,starsFor,
  async load(){ const G=A.games.smash; if(G.art) return; [G.art,G.bg]=await Promise.all([A.atlas('smash'),A.pic(A.base+'art/bg-smash.jpg?v='+(A.v||1))]);
    // the cast, for the menu card
    G.cast=['tag','boss','bag','bomb'].map(k=>{ try{ const m=G.art.map[k], c=document.createElement('canvas'); c.width=Math.round(m[2]*.5); c.height=Math.round(m[3]*.5); G.art.draw(c.getContext('2d'),k,c.width/2,c.height/2,c.width,c.height); return c.toDataURL(); }catch(e){ return ''; } }); },
  start(S,M,host){
  const g=S.cx, F=A.fx(); let W=null, lv=0, bags=[], phase='menu', raf=0, last=0, acc=0;
  const cam={x:0,y:0,k:.6,tx:0,ty:0}, aim={on:false,dx:0,dy:0}, prog=A.load('smash',{stars:[]});
  const camK=()=>Math.min(S.h/900,S.w/620);
  // the ground sits low on the screen, whatever its shape: centre height for the camera
  const gy=()=>-(S.h*.21)/cam.k;
  function screenToWorld(px,py){ return {x:(px-S.w/2)/cam.k+cam.x,y:(py-S.h*.62)/cam.k+cam.y}; }
  // ---------- menus ----------
  function menu(){ phase='menu'; const stars=prog.stars, total=stars.reduce((s,v)=>s+(v||0),0);
    const btn=i=>{ const open=i===0||(stars[i-1]||0)>0, st=stars[i]||0; return open?`<button class="lv" data-i="${i}">${i+1}<small>${'★'.repeat(st)}${'☆'.repeat(3-st)}</small></button>`:`<button class="lv lock" disabled aria-label="נעול">🔒</button>`; };
    const p=S.panel(`<div class="card"><h2>💥 מבצע הריסה</h2><div style="display:flex;justify-content:center;align-items:flex-end;gap:10px;margin:4px 0 2px">${(A.games.smash.cast||[]).map((s,i)=>s?`<img src="${s}" alt="" style="height:${[52,62,44,40][i]}px">`:'').join('')}</div><p>תגי "מחיר מלא" התבצרו מול הקניון! מותחים את הקלע, מכוונים ומפילים עליהם הכול.</p>
      <div class="grid">${LEVELS.map((_,i)=>btn(i)).join('')}</div><p>⭐ ${total} / ${LEVELS.length*3}</p><div class="row"><button class="btn ghost out">חזרה לקניון</button></div></div>`);
    p.querySelectorAll('.lv[data-i]').forEach(b=>b.onclick=()=>{ A.sfx.click(); S.panel(null); begin(+b.dataset.i); });
    p.querySelector('.out').onclick=()=>A.close(); }
  function begin(i){ lv=i; const def=LEVELS[i]; W=core(M,def); bags=def.bags.slice(); acc=0;
    // let it come to rest before anything can break, unseen
    for(let k=0;k<90;k++) W.step(); W.events.length=0;
    cam.k=camK(); cam.x=SLING.x+380; cam.y=gy(); look=0; cam.tx=cam.x; cam.ty=cam.y; phase='intro'; introT=0; F.parts.length=0; F.words.length=0; S.score(0);
    if(i===0&&!A.load('smash-help',0)) help=1; if(NEW[bags[0]]&&!seenBag(bags[0])) bagTip=bags[0]; }
  let look=0, pan=null, introT=0, help=0, bagTip=null, endT=0, slowmo=0, flyT=0;
  const NEW={trio:'שלישייה! לוחצים על המסך באוויר והיא מתפצלת לשלוש',flash:'בזק! לוחצים באוויר והוא מאיץ קדימה',safe:'כספת: כבדה ושוברת גם מתכת. לחיצה באוויר: נוחתת ישר למטה',bomb:'פצצה! מתפוצצת בפגיעה, או כשלוחצים באוויר'};
  const seenBag=k=>A.load('smash-seen-'+k,0);
  function finish(won){ phase='end'; const def=LEVELS[lv];
    if(won){ const left=bags.length; W.score+=left*10000; if(left) F.word(cam.x,cam.y-200,'+'+(left*10000)+' בונוס','#ffd166',42,1.6); }
    const st=starsFor(def,W.score,won), prev=prog.stars[lv]||0; if(st>prev){ prog.stars[lv]=st; A.save('smash',prog); }
    const coins=won?[0,6,10,15][st]+(prev?0:5):1; host.coins&&host.coins(coins); S.score(W.score);
    won?A.sfx.win():A.sfx.lose(); A.buzz(won?[30,40,30]:[60]);
    setTimeout(()=>{ const last=lv>=LEVELS.length-1;
      const p=S.panel(`<div class="card"><h2>${won?(st===3?'מושלם! 🎉':'כל הכבוד!'):'כמעט! 😤'}</h2><div class="stars">${[0,1,2].map(i=>`<span>⭐</span>`).join('')}</div>
        <p style="font-size:30px;font-weight:900;color:#fff;margin:2px 0">${W.score.toLocaleString('he-IL')}</p>
        ${won?'':'<p>נשארו תגים במקום. נסו זווית אחרת, או שקית אחרת בהתחלה.</p>'}<div class="coins">+${coins} 🪙</div>
        <div class="row">${won&&!last?'<button class="btn next">לשלב הבא ▶</button>':''}<button class="btn ${won&&!last?'ghost ':''}again">${won?'שוב לשיא':'נסו שוב'}</button><button class="btn ghost lvls">שלבים</button></div></div>`);
      const sp=[...p.querySelectorAll('.stars span')]; sp.forEach((s,i)=>{ if(i<st) setTimeout(()=>{ s.classList.add('on'); A.sfx.star(i); A.buzz(15); },250+i*320); });
      const n=p.querySelector('.next'); if(n) n.onclick=()=>{ A.sfx.click(); S.panel(null); begin(lv+1); };
      p.querySelector('.again').onclick=()=>{ A.sfx.click(); S.panel(null); begin(lv); }; p.querySelector('.lvls').onclick=()=>{ A.sfx.click(); menu(); }; },won?1100:700); }
  // ---------- the turn ----------
  function update(dt){
    if(!W) return;
    const sdt=dt*(slowmo>0?.35:1); slowmo=Math.max(0,slowmo-dt);
    acc+=sdt; let n=0; while(acc>=1/60&&n<4){ W.step(); acc-=1/60; n++; }
    W.events.forEach(ev=>{
      if(ev.k==='tag'){ ghosts.push({x:ev.x,y:ev.y,r:ev.r,a:ev.a,boss:ev.boss,t:0}); F.burst(ev.x,ev.y,['rgba(255,255,255,.9)','rgba(230,235,245,.8)'],14,240,14,-30); F.burst(ev.x,ev.y,['#c4c9d6','#8a90a3','#fff','#ff6b6b'],ev.boss?40:24,ev.boss?520:380,ev.boss?9:7); F.word(ev.x,ev.y-40,(ev.pts).toLocaleString('he-IL'),'#ffd166',ev.boss?46:36,1.2); A.sfx.pop(); A.buzz(25); F.shake=Math.max(F.shake,.5);
        if(W.alive()===0) slowmo=1.1; }
      else if(ev.k==='break'){ const P=MAT[ev.mat]; spawnDebris(ev); F.burst(ev.x,ev.y,ev.mat==='glass'?['#cdeeff','#9ed8ff','#fff']:[P.col,P.dark,'#e8d2b0'],Math.round(8+Math.sqrt(ev.w*ev.h)/6),300,ev.mat==='glass'?3:5);
        F.burst(ev.x,ev.y,['rgba(220,215,205,.7)'],6,90,10,-40); F.word(ev.x,ev.y,String(ev.pts),'#fff',22,.8); A.sfx.crack(ev.mat); F.shake=Math.max(F.shake,.25); }
      else if(ev.k==='thud'){ if(ev.v>.15) A.sfx.thud(ev.v); }
      else if(ev.k==='boom'){ F.burst(ev.x,ev.y,['#ffd166','#ff7a2e','#ff3d00','#fff'],60,700,9); F.burst(ev.x,ev.y,['rgba(60,60,70,.6)'],14,160,22,-60); A.sfx.boom(); A.buzz([50,30,50]); F.shake=1; }
      else if(ev.k==='power'){ F.burst(ev.x,ev.y,[BAG[ev.kind].col,'#fff'],18,260,5,0); A.sfx.power(); }
      else if(ev.k==='launch'){ A.sfx.launch(); trail.length=0; } });
    // the flying bag leaves puffs behind it
    const fl=W.flying; if(fl&&!fl.dead&&phase==='fly'&&W.t-fl.born<4&&fl.position.y<-4){ const q=trail[trail.length-1]; if(!q||Math.hypot(q[0]-fl.position.x,q[1]-fl.position.y)>26) trail.push([fl.position.x,fl.position.y]); }
    W.events.length=0; F.step(dt); S.score(W.score.toLocaleString('he-IL'));
    // the camera: a look at the towers first, then the slingshot; in flight it follows the bag
    cam.k+=(camK()-cam.k)*Math.min(1,dt*4);
    if(phase==='intro'){ introT+=dt; const u=Math.min(1,introT/2.4), e=u<.5?2*u*u:1-Math.pow(-2*u+2,2)/2; const far=levelCentre();
      cam.tx=far+(slingView()-far)*Math.max(0,(introT-.9)/1.5); if(introT>2.4){ phase='aim'; } cam.ty=gy(); }
    else if(phase==='aim'){ cam.tx=Math.min(levelCentre()+160,slingView()+look); cam.ty=gy(); }
    else if(phase==='fly'){ flyT+=dt; const s=W.flying, live=W.shots.filter(x=>!x.dead); const f=live.length?live.reduce((m,x)=>x.position.x>m.position.x?x:m,live[0]):null;
      if(f) cam.tx=Math.max(slingView(),Math.min(f.position.x,levelCentre()+120)); cam.ty=Math.min(gy(),f?f.position.y+80:gy());
      if(W.alive()===0&&W.quiet>.4){ finish(true); }
      else if(W.shotDone()){ W.flying=null; if(W.alive()===0) finish(true); else if(!bags.length) finish(false); else { phase='aim'; if(NEW[bags[0]]&&!seenBag(bags[0])) bagTip=bags[0]; } } }
    cam.x+=(cam.tx-cam.x)*Math.min(1,dt*(phase==='fly'?4.5:2.6)); cam.y+=(cam.ty-cam.y)*Math.min(1,dt*3); }
  const slingView=()=>SLING.x+S.w/cam.k*.28;
  const levelCentre=()=>{ const d=LEVELS[lv]; let lo=1e9,hi=-1e9; d.b.concat(d.t.map(t=>[0,t[0],0,40])).forEach(b=>{ lo=Math.min(lo,b[1]-(b[3]||0)/2); hi=Math.max(hi,b[1]+(b[3]||0)/2); }); return Math.max(slingView(),(lo+hi)/2-S.w/cam.k*.12); };
  // ---------- drawing: the painted plaza far behind, the lawn, the towers of Kenney blocks, the tags and bags ----------
  const ART=A.games.smash.art, BG=A.games.smash.bg, deb=[], ghosts=[], trail=[];
  const SPR={bag:{w:2.3,dy:-.38},trio:{w:2.7,dy:0},flash:{w:2.35,dy:-.3},safe:{w:2.3,dy:-.08},bomb:{w:2.5,dy:-.28}};
  function draw(now){ const Wd=S.w, H=S.h;
    const sh=F.shake>0?F.shake*9:0, ox=(Math.random()-.5)*sh, oy=(Math.random()-.5)*sh;
    backdrop(Wd,H,oy);
    g.save(); g.translate(Wd/2+ox,H*.62+oy); g.scale(cam.k,cam.k); g.translate(-cam.x,-cam.y);
    ground(Wd,H);
    if(W){ trailDots(); slingBack(); trajectory();
      W.blocks.forEach(b=>{ if(!b.dead) drawBlock(b); });
      debris(); W.tags.forEach(t=>{ if(!t.dead) drawTag(t,now); }); ghostTags();
      W.shots.forEach(s=>{ if(!s.dead) drawBag(s.kind,s.position.x,s.position.y,s.angle,s.circleRadius||BAG[s.kind].r,true); });
      slingFront(); queue(); }
    F.draw(g,1/cam.k*.9+.4);
    g.restore();
    if(W&&phase!=='menu') hud(Wd,H); }
  // the far plaza: the painting, mirrored side by side so it never ends, moving slower than the world
  function backdrop(Wd,H,oy){ const gy=H*.62+(0-cam.y)*cam.k+oy;
    const sky=g.createLinearGradient(0,0,0,H); sky.addColorStop(0,'#5fb0f0'); sky.addColorStop(.6,'#bfe3fb'); sky.addColorStop(1,'#a6d86d'); g.fillStyle=sky; g.fillRect(0,0,Wd,H);
    if(!BG) return; const s=Math.max(H*.95,gy/.74), top=gy-.765*s, px=-(cam.x*cam.k*.18), x0=((px%(2*s))+2*s)%(2*s)-2*s;
    for(let i=0,x=x0;x<Wd;i++,x+=s){ g.save(); if(i%2){ g.translate(x+s,top); g.scale(-1,1); } else g.translate(x,top); g.drawImage(BG,0,0,s,s); g.restore(); }
    if(top>0){ g.fillStyle='#62b2f1'; g.fillRect(0,0,Wd,top+1); } }
  // the lawn the towers stand on: a row of grass tiles, earth below
  function ground(Wd,H){ const T=56, x0=Math.floor((cam.x-Wd/cam.k)/T)*T, x1=cam.x+Wd/cam.k, yb=cam.y+H*.4/cam.k+20;
    const e=g.createLinearGradient(0,0,0,yb); e.addColorStop(0,'#c4925f'); e.addColorStop(.5,'#a8743f'); e.addColorStop(1,'#7d5228'); g.fillStyle=e; g.fillRect(x0,20,x1-x0,yb);
    if(ART.has('grass')){ for(let x=x0;x<x1;x+=T) ART.draw(g,'grass',x+T/2,T/2-10,T+.6,T); }
    else { g.fillStyle='#6fbf3b'; g.fillRect(x0,-8,x1-x0,yb); }
    g.fillStyle='rgba(0,0,0,.08)'; g.fillRect(x0,-10,x1-x0,4); }
  function slingBack(){ const x=SLING.x, h=212, w=h*307/487;
    ART.draw(g,'sling',x,-h/2+4,w,h);
    const p=pouch(); band(x+w*.3,-h*.86,p,'#5a2e14'); }
  function band(x0,y0,p,col){ g.strokeStyle=col; g.lineCap='round'; g.lineWidth=aim.on?7:9; g.beginPath(); g.moveTo(x0,y0); g.lineTo(p.x,p.y); g.stroke(); }
  function slingFront(){ const x=SLING.x, h=212, w=h*307/487, p=pouch();
    if(phase==='aim'&&bags.length){ const ang=aim.on?Math.atan2(-aim.dy,-aim.dx)*.25:0; drawBag(bags[0],p.x,p.y,ang,BAG[bags[0]].r); }
    // the leather pouch round the bag, then the near rubber
    g.fillStyle='#4a2512'; g.beginPath(); g.ellipse(p.x-(aim.on?6:0),p.y+6,10,14,0,0,7); g.fill();
    band(x-w*.32,-h*.86,p,'#7a3d1a'); }
  const pouch=()=>aim.on?{x:SLING.x+aim.dx,y:SLING.y+aim.dy}:{x:SLING.x,y:SLING.y};
  function trajectory(){ if(!aim.on||Math.hypot(aim.dx,aim.dy)<12) return; const v=W.vel(aim.dx,aim.dy); let x=SLING.x+aim.dx, y=SLING.y+aim.dy, vx=v.x, vy=v.y; const gstep=G*.001*Math.pow(1000/60,2);
    for(let i=0;i<75;i++){ vy+=gstep; x+=vx; y+=vy; if(y>0) break; if(i%4===0){ const r=Math.max(3,7-i*.06); g.fillStyle='rgba(30,40,80,.35)'; g.beginPath(); g.arc(x+1.5,y+2,r,0,7); g.fill(); g.fillStyle='#fff'; g.beginPath(); g.arc(x,y,r,0,7); g.fill(); } } }
  // the last shot's path, in puffs, until the next one flies
  function trailDots(){ g.fillStyle='rgba(255,255,255,.75)'; trail.forEach((p,i)=>{ g.beginPath(); g.arc(p[0],p[1],i%3?3:5.5,0,7); g.fill(); }); }
  function queue(){ bags.slice(phase==='aim'?1:0).forEach((k,i)=>drawBag(k,SLING.x-78-i*54,-BAG[k].r-1,0,BAG[k].r*.92)); }
  // a block: its Kenney piece stretched by the middle, so a long plank keeps crisp ends; worn as it is hurt
  function drawBlock(b){ const w=b.w, h=b.h, dmg=1-b.hp/b.max, st=dmg<.34?0:dmg<.67?1:2;
    g.save(); g.translate(b.position.x,b.position.y); g.rotate(b.angle);
    const hit=W.t-(b.hitT||-9)<.1;
    if(b.mat==='tnt'){ ART.draw(g,'tnt_q0',0,0,w,h); if(st) crack(w,h,st); }
    else { const long=w>=h*1.5, tall=h>=w*1.5, key=b.mat+'_'+(long||tall?'h':'q')+st;
      if(tall){ g.rotate(Math.PI/2); ART.nine(g,key,-h/2,-w/2,h,w,15,Math.min(w,h)*.3); }
      else if(long) ART.nine(g,key,-w/2,-h/2,w,h,15,Math.min(w,h)*.3);
      else ART.nine(g,key,-w/2,-h/2,w,h,20,Math.min(w,h)*.24); }
    if(hit){ g.globalCompositeOperation='lighter'; g.fillStyle='rgba(255,255,255,.28)'; g.fillRect(-w/2+2,-h/2+2,w-4,h-4); g.globalCompositeOperation='source-over'; }
    g.restore(); }
  function crack(w,h,st){ g.strokeStyle='rgba(40,25,10,.6)'; g.lineWidth=2; g.beginPath(); g.moveTo(-w*.3,-h*.4); g.lineTo(-w*.05,-h*.1); g.lineTo(-w*.2,h*.15); if(st>1){ g.moveTo(w*.3,h*.4); g.lineTo(w*.05,h*.12); g.lineTo(w*.22,-h*.2); } g.stroke(); }
  // a full-price tag: the angry tag (or the crowned boss), dizzy once hurt, breathing, "מחיר מלא" on its band
  function tagSprite(boss,hurt){ return (boss?'boss':'tag')+(hurt?'_hurt':''); }
  function paintTag(x,y,r,ang,boss,hurt,now,sc){ const k=tagSprite(boss,hurt), m=ART.map[k]; if(!m) return; const h=r*(boss?2.55:2.9)*(sc||1), w=h*m[2]/m[3];
    const br=1+Math.sin(now/260+x)*.025;
    g.save(); g.translate(x,y); g.rotate(ang); g.translate(0,r); g.scale(1/br,br);
    g.fillStyle='rgba(0,0,0,.18)'; g.beginPath(); g.ellipse(0,-1,w*.42,r*.16,0,0,7); g.fill();
    ART.draw(g,k,0,-h/2,w,h);
    const by=-h+h*(boss?.775:.795); g.font=`900 ${h*.088}px Rubik,system-ui,sans-serif`; g.textAlign='center'; g.textBaseline='middle'; g.direction='rtl';
    g.lineWidth=h*.02; g.strokeStyle='rgba(90,10,10,.6)'; g.strokeText('מחיר מלא',0,by,w*.78); g.fillStyle='#fff'; g.fillText('מחיר מלא',0,by,w*.78);
    g.restore(); }
  function drawTag(t,now){ paintTag(t.position.x,t.position.y,t.r,t.angle,t.boss,t.hp<t.max,now); }
  // a tag that has just gone: it swells and fades in a puff
  function ghostTags(){ for(let i=ghosts.length-1;i>=0;i--){ const q=ghosts[i]; q.t+=1/60; if(q.t>.3){ ghosts.splice(i,1); continue; } g.globalAlpha=1-q.t/.3; paintTag(q.x,q.y,q.r,q.a,q.boss,true,0,1+q.t*1.6); g.globalAlpha=1; } }
  // broken pieces: Kenney debris, tumbling and falling off
  const DEB={wood:'debw',card:'debw',glass:'debg',metal:'debs',tnt:'debs'};
  function spawnDebris(ev){ const n=Math.min(9,3+Math.round(Math.sqrt(ev.w*ev.h)/18)); for(let i=0;i<n;i++){ const a=Math.random()*6.283, v=120+Math.random()*260;
    deb.push({k:DEB[ev.mat]+(1+(Math.random()*3|0)),x:ev.x+(Math.random()-.5)*ev.w*.6,y:ev.y+(Math.random()-.5)*ev.h*.6,vx:Math.cos(a)*v,vy:Math.sin(a)*v-200,r:Math.random()*6,vr:(Math.random()-.5)*12,s:10+Math.random()*14,t:0}); } }
  function debris(){ for(let i=deb.length-1;i>=0;i--){ const d=deb[i]; d.t+=1/60; if(d.t>1.4){ deb.splice(i,1); continue; } d.vy+=900/60; d.x+=d.vx/60; d.y+=d.vy/60; d.r+=d.vr/60;
      if(d.y>-d.s/2){ d.y=-d.s/2; d.vy*=-.3; d.vx*=.6; d.vr*=.5; }
      g.save(); g.globalAlpha=Math.min(1,(1.4-d.t)*3); g.translate(d.x,d.y); g.rotate(d.r); ART.draw(g,d.k,0,0,d.s); g.restore(); } g.globalAlpha=1; }
  function drawBag(kind,x,y,ang,r,flying){ const P=SPR[kind]; let k=kind; if(kind==='bag'&&flying) k='bag_fly';
    const m=ART.map[k]; if(!m) return; const w=r*P.w, h=w*m[3]/m[2];
    g.save(); g.translate(x,y); g.rotate(ang); ART.draw(g,k,0,r*P.dy,w,h); g.restore(); }
  function hud(Wd,H){ g.save(); g.textAlign='center'; g.textBaseline='middle'; g.direction='rtl';
    const tags=W.alive(); g.fillStyle='rgba(20,24,64,.72)'; A.rr(g,12,62,128,44,14); g.fill();
    ART.draw(g,'tag',36,84,24); g.fillStyle='#fff'; g.font='900 18px Rubik,sans-serif'; g.direction='ltr'; g.fillText('× '+tags,66,85); g.direction='rtl';
    g.font='800 13px Rubik,sans-serif'; g.fillStyle='#ffd166'; g.fillText('שלב '+(lv+1),112,85);
    if(phase==='aim'&&bags.length&&!aim.on){ const m=help?'מושכים את השקית אחורה ומשחררים · החלקה מראה את המגדל':bagTip?NEW[bagTip]:null;
      if(m){ g.font='800 15px Rubik,sans-serif'; const max=Wd-48;
        // one line if it fits, else two, split at the word nearest the middle
        let lines=[m]; if(g.measureText(m).width>max){ const ws=m.split(' '); let best=1, bd=1e9; for(let i=1;i<ws.length;i++){ const d=Math.abs(g.measureText(ws.slice(0,i).join(' ')).width-g.measureText(ws.slice(i).join(' ')).width); if(d<bd){ bd=d; best=i; } } lines=[ws.slice(0,best).join(' '),ws.slice(best).join(' ')]; }
        const w=Math.min(Wd-24,Math.max(...lines.map(l=>g.measureText(l).width))+30), h=20+lines.length*20; g.fillStyle='rgba(20,24,64,.85)'; A.rr(g,(Wd-w)/2,H-28-h,w,h,14); g.fill(); g.fillStyle='#ffd166';
        lines.forEach((l,i)=>g.fillText(l,Wd/2,H-28-h+20+i*20,max)); } }
    if(phase==='fly'&&W.flying&&!W.flying.used&&W.flying.kind!=='bag'){ g.font='900 16px Rubik,sans-serif'; g.fillStyle='rgba(255,255,255,'+(.6+.4*Math.sin(performance.now()/120))+')'; g.fillText('👆 לוחצים!',Wd/2,110); }
    g.restore(); }
  // ---------- input: pull from the pouch, a tap in flight for the bag's trick ----------
  const pt=e=>{ const r=S.cv.getBoundingClientRect(); return {px:e.clientX-r.left,py:e.clientY-r.top}; };
  let drag=null;
  S.cv.addEventListener('pointerdown',e=>{ if(S.el.querySelector('.panel')) return; const {px,py}=pt(e), w=screenToWorld(px,py);
    if(phase==='fly'){ if(W.flying&&W.ability(W.flying)) A.buzz(15); return; }
    if(phase==='intro'){ introT=2.4; return; }
    if(phase!=='aim'||!bags.length) return;
    if(Math.hypot(w.x-SLING.x,w.y-SLING.y)<110/Math.min(1,cam.k*1.4)){ aim.on=true; aim.dx=0; aim.dy=0; drag=e.pointerId; try{ S.cv.setPointerCapture(e.pointerId); }catch(_){} }
    else { pan={id:e.pointerId,px,look}; } });
  // anywhere else: a swipe looks along the level (and back)
  S.cv.addEventListener('pointermove',e=>{ if(!pan||e.pointerId!==pan.id||phase!=='aim') return; const {px}=pt(e); look=Math.max(0,Math.min(levelCentre()+160-slingView(),pan.look-(px-pan.px)/cam.k)); });
  const unpan=e=>{ if(pan&&e.pointerId===pan.id) pan=null; }; S.cv.addEventListener('pointerup',unpan); S.cv.addEventListener('pointercancel',unpan);
  S.cv.addEventListener('pointermove',e=>{ if(!aim.on||e.pointerId!==drag) return; const {px,py}=pt(e), w=screenToWorld(px,py); let dx=w.x-SLING.x, dy=w.y-SLING.y; const L2=Math.hypot(dx,dy);
    if(L2>PULL){ dx*=PULL/L2; dy*=PULL/L2; } if(Math.abs(L2-Math.hypot(aim.dx,aim.dy))>14) A.sfx.stretch(Math.min(1,L2/PULL)); aim.dx=dx; aim.dy=dy; });
  const release=e=>{ if(!aim.on||e.pointerId!==drag) return; aim.on=false; drag=null; const L2=Math.hypot(aim.dx,aim.dy); if(L2<25) return;
    const v=W.vel(aim.dx,aim.dy), kind=bags.shift(); const s=W.launch(kind,v.x,v.y); Matter_setPos(s,SLING.x+aim.dx,SLING.y+aim.dy);
    if(help){ help=0; A.save('smash-help',1); } if(bagTip===kind){ A.save('smash-seen-'+kind,1); bagTip=null; } phase='fly'; flyT=0; look=0; A.buzz(12); };
  const Matter_setPos=(s,x,y)=>M.Body.setPosition(s,{x,y});
  S.cv.addEventListener('pointerup',release); S.cv.addEventListener('pointercancel',release);
  function loop(now){ const dt=Math.min(.05,(now-last)/1000||0); last=now; if(phase!=='menu') update(dt); draw(now); raf=requestAnimationFrame(loop); }
  menu(); last=performance.now(); raf=requestAnimationFrame(loop);
  return {resize(){}, stop(){ cancelAnimationFrame(raf); }, debug:{W:()=>W,begin,phase:()=>phase,aim,bags:()=>bags,cam,
    shoot(dx,dy){ aim.on=true; aim.dx=dx; aim.dy=dy; release({pointerId:drag=1}); }, tap(){ W.flying&&W.ability(W.flying); }, finish}};
}};
})();
