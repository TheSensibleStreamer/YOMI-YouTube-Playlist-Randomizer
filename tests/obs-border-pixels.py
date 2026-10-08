#!/usr/bin/env python3
"""Pixel-level browser test of YOMI's actual OBS HTML. Install playwright and pillow first."""
import asyncio
import io
import json
import os
import re
from pathlib import Path
from PIL import Image
from playwright.async_api import async_playwright

SOURCE = Path(__file__).resolve().parents[1] / "payload/app/YomiObsServerHost.cs"

def overlay_html():
    src = SOURCE.read_text(encoding="utf-8-sig")
    match = re.search(
        r'private static string OverlayHtml\(SimpleRequest req\)\s*\{\s*'
        r'return "((?:\\.|[^"\\])*)"\+YomiOverlayFontFacesCss\(\)\+'
        r'"((?:\\.|[^"\\])*)";', src, re.S)
    assert match, "Cannot extract actual OBS HTML from C#"
    html = json.loads('"'+match.group(1)+'"') + json.loads('"'+match.group(2)+'"')
    assert html.count('tick();\n})();') == 1
    return html.replace('tick();\n})();',
                        'window.__yomiPixelTestApply=apply;\ntick();\n})();')

async def check(page, border, corner, layout, with_video=True, video_aspect=16/9):
    config = {
        "app_mode": "Streamer / OBS", "overlay_width": 2560,
        "overlay_height": 144, "canvas_width": 2560, "canvas_height": 144,
        "media_width": 256, "media_height": 144, "media_border_enabled": True,
        "media_border_color": "#252525", "media_border_px": border,
        "media_corner_style": corner, "media_aspect_layout": layout,
        "artwork_enabled": True, "video_enabled": with_video,
        "title_enabled": True, "channel_enabled": True,
        "visualizer_enabled": True, "visualizer_match_text_overhang": True,
        "overlay_text_gap_px": 8
    }
    track = {
        "index": 1, "title": "Long title testing visualizer width",
        "channel": "Music - Topic", "artwork": "unavailable",
        "full_artwork": "unavailable", "visualizer": "unavailable"
    }
    if abs(video_aspect - 16/9) > 0.025:
        track["video"] = "unavailable"  # Forces actual video aspect handling instead of full-art fallback.
    await page.evaluate("ratio => { videoAspect=ratio; }", video_aspect)
    await page.evaluate("""([config,track])=>{
        window.__yomiPixelTestApply(config,track,{});
        document.getElementById('artFrame').style.backgroundColor='red';
        document.getElementById('vidFrame').style.backgroundColor='blue';
    }""", [config,track])
    await page.wait_for_timeout(50)
    boxes = await page.evaluate("""()=>{
       function box(id){let r=document.getElementById(id).getBoundingClientRect();
         return {x:r.x,y:r.y,w:r.width,h:r.height};}
       return {art:box('artFrame'),vid:box('vidFrame'),
               artModule:box('art'),vidModule:box('vid'),
               radii:['artFrame','vidFrame'].map(id=>{let st=getComputedStyle(document.getElementById(id));return [st.borderTopLeftRadius,st.borderTopRightRadius,st.borderBottomLeftRadius,st.borderBottomRightRadius];}),
               text:box('text'),viz:box('viz'),
               svg:getComputedStyle(document.getElementById('mediaPairSvg')).display};
    }""")
    image = Image.open(io.BytesIO(await page.screenshot(omit_background=True))).convert("RGBA")
    if with_video and layout == "Reflow" and abs(video_aspect - 16/9)<0.025:
        a,v=boxes["art"],boxes["vid"]
        assert round(a["x"]+a["w"]-v["x"]) == border, (border,boxes)
        assert boxes["svg"]=="none", "Shared SVG hides transparent inner corners"
        y=round(a["y"]+a["h"]/2)
        seam=[image.getpixel((x,y)) for x in range(round(v["x"]),round(a["x"]+a["w"]))]
        assert len(seam)==border and all(px==(37,37,37,255) for px in seam), seam
    for radii in boxes['radii'][:2 if with_video else 1]:
        values=[float(v.removesuffix('px')) for v in radii]
        if corner == 'Square':
            assert all(v == 0 for v in values), (corner,values,boxes)
        else:
            assert all(v >= 6.4 for v in values), (border,corner,layout,values,boxes)
    if with_video and video_aspect < 1.4:
        assert boxes['vid']['w'] < boxes['art']['w'], boxes
        assert boxes['vid']['x'] >= boxes['art']['x'] + boxes['art']['w'] - 1, boxes
        if layout == 'Fixed':
            assert abs(boxes['vidModule']['w']-boxes['artModule']['w']) < 1, boxes
        else:
            assert abs(boxes['vidModule']['w']-boxes['vid']['w']) < 1, boxes
    if corner != "Square":
        for name in (["art","vid"] if with_video else ["art"]):
            a=boxes[name]
            for x in (round(a["x"]),round(a["x"]+a["w"])-1):
                for y in (round(a["y"]),round(a["y"]+a["h"])-1):
                    assert image.getpixel((x,y))[3]==0, (border,corner,layout,name,(x,y),image.getpixel((x,y)))
    if with_video:
        assert boxes["viz"]["w"]>=boxes["text"]["w"]-2, boxes
    print(f"PASS {border}px {corner} {layout} video={with_video}, video_ratio={video_aspect:.3f}, text={boxes['text']['w']:.0f}px viz={boxes['viz']['w']:.0f}px")

async def main():
    async with async_playwright() as p:
        browser=await p.chromium.launch(headless=True,executable_path=os.environ.get("YOMI_TEST_CHROMIUM") or None)
        try:
            page=await browser.new_page(viewport={"width":1400,"height":144})
            await page.add_init_script("window.fetch=async()=>new Promise(()=>{});")
            errors=[]
            page.on("pageerror",lambda e:errors.append(str(e)))
            await page.set_content(overlay_html(),wait_until="domcontentloaded")
            await page.wait_for_function("window.__yomiPixelTestApply != null")
            for args in [(2,"Soft","Reflow",True),(4,"Soft","Reflow",True),
                         (8,"Soft","Reflow",True),(2,"Rounded","Reflow",True),
                         (2,"Soft","Fixed",True),(2,"Soft","Reflow",False),
                         (2,"Square","Reflow",True),(2,"Soft","Reflow",True,4/3),
                         (2,"Rounded","Fixed",True,4/3)]:
                await check(page,*args)
            assert not errors,errors
        finally:
            await browser.close()

if __name__=="__main__":
    asyncio.run(main())
