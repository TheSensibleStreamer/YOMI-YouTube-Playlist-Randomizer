// Validate production OBS Browser Source code, not a rewritten implementation.
const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');

const source = fs.readFileSync('payload/app/YomiObsServerHost.cs', 'utf8');
const start = source.indexOf('return "<!doctype html>');
const end = source.indexOf('</html>";', start);
assert(start !== -1 && end > start, 'embedded OBS HTML found');
const expression = source.slice(start + 'return '.length, end + '</html>"'.length);
const html = new Function('YomiOverlayFontFacesCss', 'return ' + expression)(() => '');
const scripts = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
assert.equal(scripts.length, 1, 'exactly one OBS script');
const js = scripts[0][1];
new vm.Script(js, { filename: 'Yomi-OBS-browser.js' });

function section(from, to) {
  const a = js.indexOf(from), b = js.indexOf(to, a + from.length);
  assert(a >= 0 && b > a, 'production function section found: ' + from);
  return js.slice(a, b);
}
const clock = section('function clockPositionAt(', 'function layoutMediaPair(');
const grid = section('function visualizerColumns(', 'function layoutViz(');
const vizRenderer=section('function renderVizFrame()', 'function ensureCanvas(');
const palette=section('function parseHexColor(', 'function renderVizFrame(');
assert(!clock.includes('fetch('), 'no HTTP requests in presented-frame sync loop');
assert(!js.includes('clockTick('), 'no 80 ms clock polling loop');
assert(js.includes('syncAV(videoEl,\'video\',null);syncAV(vizEl,\'viz\',null);'), 'both sources synchronize to same clock');
assert(js.includes("watchFrames(videoEl,'video');watchFrames(vizEl,'viz');"), 'both sources use presented frame callbacks');
assert(js.includes('vizSourceCtx.drawImage(vizEl,0,0,sw,sh);'), 'full-width native source grid is sampled before pooling');
assert(vizRenderer.includes('hi=Math.max(lo+1,Math.ceil((x+1)*sw/w))'), 'frequency bins peak-pooled, not dropped');
assert(!js.includes('vizLengthMultiplier/4'), 'no old 4.0 ceiling on length');

const context = {
  Math, Number, String,
  lastClock: { connected: true, time: 12.3, age_ms: 24, paused: false, speed: 1 },
  lastClockAt: 1000,
  lastVideo: '/video', lastViz: '/visualizer',
  lastSeek: { video: 0, viz: 0 },
  lastDriftMs: { video: null, viz: null },
  performance: {now: () => 1000},
  num: (v, d) => Number.isFinite(Number(v)) ? Number(v) : d,
  bool: (v, d) => typeof v === 'boolean' ? v : d,
  viz: { classList: {contains:()=>false} },
  vid: { classList: {contains:()=>false} },
};
vm.createContext(context);
vm.runInContext(clock + grid + palette, context);

function el(time = 0) {
  return {
    readyState: 4, duration: 120, currentTime: time, paused: true,
    seeking: false, playbackRate: 1,
    dataset: {yomiStable: '0'}, style: {visibility: 'hidden'},
    play() {this.paused=false; return {catch(){}};},
    pause() {this.paused=true;},
  };
}
let video=el(), visualizer=el();
context.syncAV(video, 'video', null);
context.syncAV(visualizer, 'viz', null);
assert.equal(video.currentTime, visualizer.currentTime, 'both start at same audio clock');
assert(Math.abs(video.currentTime - 12.324) < .002);
assert.equal(video.paused,false); assert.equal(visualizer.paused,false);
assert.equal(video.style.visibility, 'visible');
assert.equal(visualizer.style.visibility, 'visible');

context.lastClock.paused=true;
context.syncAV(video,'video',null);
context.syncAV(visualizer,'viz',null);
assert.equal(video.paused,true); assert.equal(visualizer.paused,true);
context.lastClock.paused=false;
context.lastClock.time=14.1;
context.lastClockAt=1000;
context.syncAV(video,'video',null);
context.syncAV(visualizer,'viz',null);
assert.equal(video.paused,false); assert.equal(visualizer.paused,false);
assert.equal(video.currentTime, visualizer.currentTime, 'same clock after pause and resume');

const fitTextGrid=context.fitVizTextGrid;
for(const [w,h,cols,rows,ew,eh,n] of [
 [720,90,192,8,715,88,65],
 [300,90,192,8,297,88,27],
 [190,90,192,8,187,88,17],
 [7,90,192,8,7,56,1]
]) {
 const o=fitTextGrid(w,h,cols,rows);
 assert.deepEqual(Array.from(o),[ew,eh,n],'text-match tracks requested text width');
 assert.equal(o[0]/o[2],o[1]/rows,'square cells at every dynamic width');
}
const titleEl={classList:{contains:()=>false},textContent:'Long title',firstChild:{},glyph:{left:525,right:855,width:330}};
const channelEl={classList:{contains:()=>false},textContent:'Short channel',firstChild:{},glyph:{left:525,right:685,width:160}};
context.titleEl=titleEl;context.channelEl=channelEl;
context.document={createRange:()=>({selectNodeContents(el){this.el=el},getBoundingClientRect(){return this.el.glyph}})};
let textExtent=context.visibleVizTextBounds({left:520,right:1390});
assert.equal(textExtent.left,525);
assert.equal(textExtent.right,855,'longer title sets rightmost edge');
channelEl.glyph={left:525,right:990,width:465};
textExtent=context.visibleVizTextBounds({left:520,right:1390});
assert.equal(textExtent.right,990,'longer channel sets rightmost edge');
channelEl.classList.contains=()=>true;
textExtent=context.visibleVizTextBounds({left:520,right:1390});
assert.equal(textExtent.right,855,'hidden channel does not influence width');
const fit=context.fitVizPixelGrid;
const bins=context.visualizerColumns;
const measured=[];
for(const n of [1,2,4,8]) {
 const width=120*n,cols=bins(192,n),shape=fit(2560,90,cols,8,width);
 measured.push(shape[0]);
 assert.equal(shape[1],88,'height remains fixed across manual length '+n);
 assert(Math.abs(shape[0]-width)<=11,'manual '+n+' produces expected compact width, got '+shape[0]);
 assert.equal(shape[0]/shape[2],shape[1]/8,'all pixels square at length '+n);
}
assert(measured[0]<150 && measured[1]<300 && measured[3]<1000,'regression: 1.0 and 2.0 must not fill screen: '+measured);
const widths=[1,2,4,8].map(n=>fit(880,135,bins(192,n),8,120*n));
assert(widths.every((v,i)=>i===0||v[0]>=widths[i-1][0]),'length must not shrink on a small source');
assert(widths.every(v=>v[1]===128),'height stays constant even when length is capped to viewport');
// Exercise actual production per-frame renderer: a peak in an odd-numbered
// source column MUST remain visible when default length reduces 192 to 96.
const sw=192,sh=8;
const raw=new Uint8ClampedArray(sw*sh*4);
let brightAt=(2*sw+13)*4;raw[brightAt]=180;raw[brightAt+1]=110;raw[brightAt+2]=60;raw[brightAt+3]=255;
const put=[];
context.vizCtx={createImageData:(w,h)=>({width:w,height:h,data:new Uint8ClampedArray(w*h*4)}),putImageData:(frame)=>{put.push(frame)}};
context.vizSourceCtx={imageSmoothingEnabled:false,drawImage:()=>{},getImageData:()=>({data:raw})};
context.vizCanvas={width:0,height:0};
context.vizSourceCanvas={width:0,height:0};
context.vizEl={videoWidth:sw,videoHeight:sh};
context.vizDisplayColumns=0;
context.vizAutoMatchText=false;
context.vizLengthMultiplier=4;
context.vizColorMode='solid';context.vizSolidColor='#7F40F0';
context.vizGradientPreset='Sunset';context.vizGradientOrientation='Horizontal';
context.vizPaletteCache=null;context.vizPaletteCacheKey='';

vm.runInContext(vizRenderer,context);
context.renderVizFrame();
assert.equal(put.length,1);
let frame=put[0];assert.equal(frame.width,96);assert.equal(frame.height,8);
let at=(2*96+6)*4;
assert.deepEqual(Array.from(frame.data.slice(at,at+4)),[127,64,240,255],'odd source-column transient is preserved');
context.vizLengthMultiplier=8;
context.vizDisplayColumns=183; // Narrow OBS source requires peak pooling to integer square cells.
context.renderVizFrame();
assert.equal(put[1].width,183,'8.0 fills narrow source without elongated cells or unused whitespace');
context.vizDisplayColumns=0;
context.renderVizFrame();
assert.equal(put[2].width,192,'full-width OBS source uses all 8.0 units without skipped bins');
context.vizLengthMultiplier=1;
context.vizAutoMatchText=true;
context.vizDisplayColumns=150;
context.renderVizFrame();
assert.equal(put[3].width,150,'automatic match ignores the manual 1.0 length cap');
context.vizAutoMatchText=false;
context.vizLengthMultiplier=8;
context.vizColorMode='gradient';context.vizGradientPreset='Ocean';context.vizGradientOrientation='Horizontal';
context.vizDisplayColumns=80;
context.renderVizFrame();
const oldColor=Array.from(put[4].data.slice((2*80+5)*4,(2*80+5)*4+4));
context.vizGradientPreset='Fire';
context.renderVizFrame();
const newColor=Array.from(put[5].data.slice((2*80+5)*4,(2*80+5)*4+4));
assert.notDeepEqual(oldColor,newColor,'changing gradient instantly recolors the current cached frame');
context.vizGradientPreset='Rainbow';
context.renderVizFrame();
assert.notDeepEqual(newColor,Array.from(put[6].data.slice((2*80+5)*4,(2*80+5)*4+4)),'rainbow is a gradient preset');
const faintAt=(3*sw+20)*4;raw[faintAt]=24;raw[faintAt+1]=24;raw[faintAt+2]=24;
context.renderVizFrame();
const faint=put[7].data.slice((3*80+8)*4,(3*80+8)*4+4);
assert.equal(faint[3],0,'near-black codec residue must not light bottom rows');
// Startup geometry regression: before a first track/font has measurable
// text, the OBS visualizer may not sit beneath media. Once text becomes
// measurable, the existing dynamic sizing rule must remain unchanged.
vm.runInContext(section('function layoutViz(', 'function apply('), context);
context.root={getBoundingClientRect:()=>({left:0,top:0,right:1400,bottom:90,width:1400,height:90})};
context.artFrame={getBoundingClientRect:()=>({left:0,right:160,top:0,bottom:90})};
context.vidFrame={getBoundingClientRect:()=>({left:160,right:320,top:0,bottom:90})};
context.viz={style:{},classList:{contains:()=>false}};
context.txt={style:{},getBoundingClientRect:()=>({left:328,right:1392,top:0,bottom:90,width:0,height:0})};
context.vizManualBaseWidth=120;context.vizLengthMultiplier=4;
context.layoutViz(true,true,'Behind text',true,1, false,true,true,false);
assert.equal(context.viz.style.left,'320px','zero-sized text on OBS startup must not send viz behind media');
assert.equal(context.viz.style.width,'0px','unmeasured startup text must not draw a stale full-width spectrum');
context.txt.getBoundingClientRect=()=>({left:328,right:1392,top:0,bottom:90,width:1064,height:90});
titleEl.classList.contains=()=>false;
titleEl.glyph={left:336,right:795,width:459};
channelEl.classList.contains=()=>false;
channelEl.glyph={left:336,right:600,width:264};
context.layoutViz(true,true,'Behind text',true,1,false,true,true,false);
assert.equal(context.viz.style.left,'320px','dynamic spectrum stays flush with last visible media pixel');
assert.equal(context.vizAutoMatchText,true,'dynamic length is available after metadata finishes');
const vizRight=320+parseFloat(context.viz.style.width);
assert(Math.abs(vizRight-803)<=11,'dynamic size includes symmetrical 16px media-to-glyph overhang');
context.vizLengthMultiplier=8;
context.layoutViz(true,true,'Behind text',false,1,false,true,true,false);
const manualRight=320+parseFloat(context.viz.style.width);
assert(manualRight>1350 && manualRight<=1392,
  'manual 8.0 should span nearly the full usable OBS overlay with square frequency pixels');

console.log('PASS OBS shared clock, 1-8 visualizer width, coarser square Extra Chunky grid, peak pooling and full 60 FPS frame shape');
