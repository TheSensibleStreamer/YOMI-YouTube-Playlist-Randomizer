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
                        'window.__yomiPixelTestApply=apply;\nwindow.__yomiPixelTestAspect=(ratio)=>{videoAspect=ratio;};\nwindow.__yomiPixelTestRenderViz=renderVizFrame;\ntick();\n})();')

async def check(page, border, corner, layout, with_video=True, video_aspect=16/9, with_art=True):
    config = {
        "app_mode": "Streamer / OBS", "overlay_width": 2560,
        "overlay_height": 135, "canvas_width": 2560, "canvas_height": 144,
        "media_width": 256, "media_height": 144, "media_border_enabled": True,
        "media_border_color": "#252525", "media_border_px": border,
        "media_corner_style": corner, "media_aspect_layout": layout,
        "artwork_enabled": with_art, "video_enabled": with_video,
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
    await page.evaluate("ratio => window.__yomiPixelTestAspect(ratio)", video_aspect)
    await page.evaluate("""([config,track])=>{
        window.__yomiPixelTestApply(config,track,{});
        document.getElementById('artFrame').style.backgroundColor='red';
        document.getElementById('vidFrame').style.backgroundColor='blue';
    }""", [config,track])
    await page.wait_for_timeout(50)
    pixel_info = await page.evaluate("""()=>{
        Object.defineProperty(vizEl, "videoWidth", {configurable:true, value:40});
        Object.defineProperty(vizEl, "videoHeight", {configurable:true, value:10});
        window.__yomiPixelTestRenderViz();
        let box=vizCanvas.getBoundingClientRect();
        return {w:box.width,h:box.height,srcW:vizCanvas.width,srcH:vizCanvas.height};
    }""")
    assert pixel_info["srcW"] > 0 and pixel_info["srcH"] == 10, pixel_info
    physical_pixel_x=pixel_info["w"]/pixel_info["srcW"]
    physical_pixel_y=pixel_info["h"]/pixel_info["srcH"]
    assert abs(physical_pixel_x/physical_pixel_y-1) < 0.06, (pixel_info,physical_pixel_x,physical_pixel_y)
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
    if with_video and layout == "Reflow":
        a,v=boxes["art"],boxes["vid"]
        # Zero image overlap is essential: otherwise artwork bleeds through
        # the transparent TOP/BOTTOM corners of the right media frame.
        assert abs(a["x"]+a["w"]-v["x"]) < 0.6, (border,boxes)
        assert boxes["svg"]=="none", "Shared SVG hides transparent inner corners"
        y=round(a["y"]+a["h"]/2)
        seam_x=round(v["x"])
        seam=[image.getpixel((x,y)) for x in range(seam_x-border,seam_x+border)]
        assert sum(px==(37,37,37,255) for px in seam)==border, (border,seam)
        if corner != "Square":
            # Inner top corner pixels must show pure transparent OBS background.
            for frame in (a,v):
                xx=round(frame["x"]+frame["w"])-1 if frame is a else round(frame["x"])
                assert image.getpixel((xx,round(frame["y"])))[3]==0, (frame,(xx,round(frame["y"])))
                assert image.getpixel((xx,round(frame["y"]+frame["h"])-1))[3]==0, (frame,(xx,round(frame["y"]+frame["h"])-1))
    visible = (["art"] if with_art else []) + (["vid"] if with_video else [])
    for name in visible:
        assert boxes[name]["h"] <= 135.1, ("Rounded border clipped by browser height",name,boxes[name])
    for radii in boxes['radii'][:2 if with_video else 1]:
        values=[float(v.removesuffix('px')) for v in radii]
        if corner == 'Square':
            assert all(v == 0 for v in values), (corner,values,boxes)
        else:
            assert all(v >= 6.4 for v in values), (border,corner,layout,values,boxes)
    if with_video and video_aspect < 1.4:
        assert boxes['vid']['w'] < boxes['art']['w'], boxes
        if layout == 'Fixed':
            assert boxes['vid']['x'] >= boxes['art']['x'] + boxes['art']['w'] - 1, boxes
            assert abs(boxes['vidModule']['w']-boxes['artModule']['w']) < 1, boxes
        else:
            assert abs(boxes['vidModule']['w']-boxes['vid']['w']) < 1, boxes
    if corner != "Square":
        for name in visible:
            a=boxes[name]
            for x in (round(a["x"]),round(a["x"]+a["w"])-1):
                for y in (round(a["y"]),round(a["y"]+a["h"])-1):
                    assert image.getpixel((x,y))[3]==0, (border,corner,layout,name,(x,y),image.getpixel((x,y)))
    if with_video:
        assert boxes["viz"]["w"]>=boxes["text"]["w"]-2, boxes
    visualizer_left=boxes["viz"]["x"]
    if visible:
        last_frame=boxes[visible[-1]]
        last_visible_pixel=last_frame["x"]+last_frame["w"]
    else:
        last_visible_pixel=boxes["text"]["x"]
    assert abs(visualizer_left-last_visible_pixel)<1, ("visualizer behind media",boxes)
    # In the normal 16:9 reflow case, text starts exactly the configured
    # moduleGap after the visualizer's first pixel, not 100s of pixels away.
    if layout=="Reflow" and abs(video_aspect-16/9)<0.025 and visible:
        assert abs((boxes["text"]["x"]-visualizer_left)-8)<1.1, boxes
    print(f"PASS {border}px {corner} {layout} video={with_video}, video_ratio={video_aspect:.3f}, text={boxes['text']['w']:.0f}px viz={boxes['viz']['w']:.0f}px")

async def main():
    async with async_playwright() as p:
        browser=await p.chromium.launch(headless=True,executable_path=os.environ.get("YOMI_TEST_CHROMIUM") or None)
        try:
            page=await browser.new_page(viewport={"width":1400,"height":135})
            await page.add_init_script("window.fetch=async()=>new Promise(()=>{});")
            errors=[]
            page.on("pageerror",lambda e:errors.append(str(e)))
            await page.set_content(overlay_html(),wait_until="domcontentloaded")
            await page.wait_for_function("window.__yomiPixelTestApply != null")
            for args in [(1,"Soft","Reflow",True),(2,"Soft","Reflow",True),
                         (4,"Soft","Reflow",True),(8,"Soft","Reflow",True),
                         (2,"Rounded","Reflow",True),(2,"Soft","Fixed",True),
                         (2,"Soft","Reflow",False),(2,"Square","Reflow",True),
                         (2,"Soft","Reflow",True,4/3),(2,"Rounded","Fixed",True,4/3),
                         (2,"Soft","Reflow",True,16/9,False),
                         (2,"Soft","Reflow",False,16/9,False)]:
                await check(page,*args)
            assert not errors,errors
        finally:
            await browser.close()

if __name__=="__main__":
    asyncio.run(main())
