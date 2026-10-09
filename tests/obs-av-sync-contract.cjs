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
const grid = section('function fitVizPixelGrid(', 'function layoutViz(');
assert(!clock.includes('fetch('), 'no HTTP requests in presented-frame sync loop');
assert(!js.includes('clockTick('), 'no 80 ms clock polling loop');
assert(js.includes('syncAV(videoEl,\'video\',null);syncAV(vizEl,\'viz\',null);'), 'both sources synchronize to same clock');
assert(js.includes("watchFrames(videoEl,'video');watchFrames(vizEl,'viz');"), 'both sources use presented frame callbacks');
assert(js.includes('vizCtx.drawImage(vizEl,0,0,w,sh,0,0,w,h);'), 'source pixel grid is cropped, not stretched');

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

const fit=context.fitVizPixelGrid;
for(const [vw,vh,cols,rows,ew,eh] of [
 [2560,90,40,10,360,90],
 [2000,90,64,16,320,80],
 [2560,90,96,24,288,72],
 [180,90,40,10,160,40],
 [80,90,40,10,80,20]
]) {
 const actual=fit(vw,vh,cols,rows);
 assert.equal(actual[0],ew,'width for ' + cols + ' x ' + rows);
 assert.equal(actual[1],eh,'height for ' + cols + ' x ' + rows);
 assert(Math.abs(actual[0]/cols-actual[1]/rows)<1e-8,'square output cells');
}
console.log('PASS OBS embedded JS syntax; common audio clock and pause/resume; no frame-time HTTP; square-pixel geometry');
