#!/usr/bin/env python3
"""Render the ACTUAL production OBS pixel-grid function in Chrome.

Screenshots, not mere CSS arithmetic, validate 1:1 physical pixel cells at
100%, 125%, 150% and 200% Windows-style display scaling.
"""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from PIL import Image

source = Path("payload/app/YomiObsServerHost.cs").read_text(encoding="utf-8-sig")
start = source.index("function vizDeviceScale()")
end = source.index("function visibleVizTextBounds(", start)
grid_code = source[start:end].replace(r"\n", "\n")
assert "function fitVizPixelGrid(" in grid_code
assert "cellPhysical" in grid_code

chrome = next((shutil.which(n) for n in ["google-chrome", "google-chrome-stable", "chromium", "chromium-browser"] if shutil.which(n)), None)
if not chrome:
    raise SystemExit("Chrome/Chromium not installed on browser test runner")

out_dir = Path("dist/pixel-screenshots")
out_dir.mkdir(parents=True, exist_ok=True)

def runs(binary_values):
    runs = []
    last, count = None, 0
    for v in binary_values:
        v = int(v > 127)
        if last is None or v == last:
            count += 1
        else:
            runs.append((last, count))
            count = 1
        last = v
    runs.append((last, count))
    return runs

for dpr in (1.0, 1.25, 1.5, 2.0):
    with tempfile.TemporaryDirectory(prefix="yomi-chromium-") as temp:
        root = Path(temp)
        html = root / "index.html"
        html.write_text("""<!doctype html><meta charset="utf-8"><style>
html,body{margin:0;background:#101010}
#viz{position:absolute;left:80px;top:50px;overflow:hidden;display:flex}
#viz canvas{display:block;image-rendering:pixelated;background:transparent}
</style><div id="viz"><canvas id="vizCanvas"></canvas></div><script>
""" + grid_code + """
const [width,height,cols]=fitVizPixelGrid(1024,135,192,9,570);
const parent=document.getElementById('viz'),c=document.getElementById('vizCanvas');
parent.style.width=width+'px';parent.style.height=height+'px';
c.style.width=width+'px';c.style.height=height+'px';
c.width=cols;c.height=9;
const ctx=c.getContext('2d',{alpha:false});
for(let y=0;y<9;y++)for(let x=0;x<cols;x++){
ctx.fillStyle=(x+y)%2===0?'#ffffff':'#000000';ctx.fillRect(x,y,1,1);
}
</script>""",encoding="utf-8")
        dest = out_dir / ("actual-obs-dpr-" + str(dpr).replace(".","-") + ".png")
        cmd = [chrome, "--headless=new", "--no-sandbox", "--disable-gpu",
               "--disable-dev-shm-usage", "--no-first-run", "--hide-scrollbars",
               "--disable-extensions", "--disable-background-networking",
               "--force-device-scale-factor=" + str(dpr),
               "--window-size=1180,420", "--virtual-time-budget=1200",
               "--user-data-dir=" + str(root / "profile"),
               "--screenshot=" + str(dest.resolve()), html.resolve().as_uri()]
        try:
            proc = subprocess.run(cmd, capture_output=True, text=True, timeout=35)
        except subprocess.TimeoutExpired as e:
            raise AssertionError("Chrome screenshot timed out at DPI "+str(dpr)) from e
        if proc.returncode or not dest.is_file():
            raise AssertionError(f"Chrome screenshot failed at DPI {dpr}: {proc.stderr[-600:]}")
        im = Image.open(dest).convert("RGB")
        x0, y0 = round(80*dpr), round(50*dpr)
        physical_cell = max(1,int((135*dpr)//9))
        y_sample = y0 + physical_cell//2
        x_sample = x0 + physical_cell//2
        # Sample checkerboard interiors through eight native cells. A scaled
        # 33-column bitmap stretched over 38-column CSS produces unequal runs.
        horizontal = [im.getpixel((x,y_sample))[0] for x in range(x0+2, x0+physical_cell*8-2)]
        vertical = [im.getpixel((x_sample,y))[0] for y in range(y0+2,y0+physical_cell*8-2)]
        hruns, vruns = runs(horizontal), runs(vertical)
        # Discard first/last shortened runs due to our deliberate +2 crop.
        hv = [n for _,n in hruns[1:-1]]
        vv = [n for _,n in vruns[1:-1]]
        assert len(hv)>=5 and len(vv)>=5, (dpr,hruns,vruns)
        for dimension, samples in (("horizontal",hv),("vertical",vv)):
            assert all(abs(n-physical_cell)<=1 for n in samples), \
                (dpr,dimension,physical_cell,samples)
        assert abs(sum(hv)/len(hv)-sum(vv)/len(vv))<=1.0, (dpr,hv,vv)
        print(f"PASS real Chromium screenshot {dpr*100:.0f}% DPI: {physical_cell}x{physical_cell} physical-pixel cells")
print("PASS: rendered OBS pixels are physically square across four display scales")
