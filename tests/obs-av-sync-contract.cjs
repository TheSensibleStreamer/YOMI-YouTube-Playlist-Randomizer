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
vm.runInContext(clock + grid, context);

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
 [720,90,192,6,720,90,48],
 [300,90,192,6,300,90,20],
 [190,90,192,6,180,90,12],
 [7,90,192,6,7,42,1]
]) {
 const o=fitTextGrid(w,h,cols,rows);
 assert.deepEqual(Array.from(o),[ew,eh,n],'text-match uses full-height square cells');
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
for(const [vw,vh,cols,rows,ew,eh,ec] of [
 [2560,90,24,6,360,90,24],
 [2560,90,48,6,720,90,48],
 [2560,90,96,6,1440,90,96],
 [2200,90,192,6,2196,72,183],
 [2000,90,64,16,320,80,64],
 [2560,90,96,24,288,72,96],
 [180,90,40,10,180,50,36],
 [80,90,40,10,80,20,40]
]) {
 const actual=fit(vw,vh,cols,rows);
 assert.equal(actual[0],ew,'width for ' + cols + ' x ' + rows);
 assert.equal(actual[1],eh,'height for ' + cols + ' x ' + rows);
 assert.equal(actual[2],ec,'displayed column count');
 assert(Math.abs(actual[0]/actual[2]-actual[1]/rows)<1e-8,'square output cells');
}

const bins=context.visualizerColumns;
for(const [maxColumns,length,expected] of [
 [192,1,24],[192,2,48],[192,4,96],[192,6,144],[192,8,192],
 [128,4,64],[128,8,128]
]) assert.equal(bins(maxColumns,length),expected,'length '+length+' maps correctly');
assert.equal(fit(2560,90,bins(192,4),6)[0],1440,'length 4.0 uses 1440 pixels at 90 high');
assert.equal(fit(2200,90,bins(192,8),6)[0],2196,'length 8.0 fills 2196 of 2200 pixels with square cells');
const widths=[1,2,4,8].map(n=>fit(880,135,bins(192,n),6)[0]);
assert(widths.every((w,i)=>i===0||w>=widths[i-1]),'length must not get shorter in a narrow OBS source: '+widths);

// Exercise actual production per-frame renderer: a peak in an odd-numbered
// source column MUST remain visible when default length reduces 192 to 96.
const sw=192,sh=6;
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
context.parseHexColor=()=>[127,64,240];
vm.runInContext(vizRenderer,context);
context.renderVizFrame();
assert.equal(put.length,1);
let frame=put[0];assert.equal(frame.width,96);assert.equal(frame.height,6);
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
console.log('PASS OBS shared clock, 1-8 visualizer width, coarser square Extra Chunky grid, peak pooling and full 60 FPS frame shape');
