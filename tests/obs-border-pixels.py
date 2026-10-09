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
                        'window.__yomiPixelTestApply=apply;\nwindow.__yomiPixelTestAspect=(ratio)=>{videoAspect=ratio;};\nwindow.__yomiPixelTestRenderViz=renderVizFrame;\nwindow.__yomiPixelTestAutoMatch=()=>vizAutoMatchText;\nwindow.__yomiPixelTestColumns=()=>vizDisplayColumns;\ntick();\n})();')

async def check(page, border, corner, layout, with_video=True, video_aspect=16/9, with_art=True, viz_length=4):
    config = {
        "app_mode": "Streamer / OBS", "overlay_width": 2560,
        "overlay_height": 135, "canvas_width": 2560, "canvas_height": 144,
        "media_width": 256, "media_height": 144, "media_border_enabled": True,
        "media_border_color": "#252525", "media_border_px": border,
        "media_corner_style": corner, "media_aspect_layout": layout,
        "artwork_enabled": with_art, "video_enabled": with_video,
        "title_enabled": True, "channel_enabled": True,
        "visualizer_enabled": True, "visualizer_match_text_overhang": False,
        "overlay_text_gap_px": 8, "visualizer_length_multiplier": viz_length
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
        // Exercise REAL media pixels, not only colored frame backgrounds.
        // A 1px tall photo/video strip that escapes the border is a regression.
        const solid=(color)=>'data:image/svg+xml,'+encodeURIComponent(
          '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="144"><rect width="256" height="144" fill="'+color+'"/></svg>');
        artImg.src=solid('#ff0000');
        videoFullArt.src=solid('#0000ff');
        document.getElementById('artFrame').style.backgroundColor='red';
        document.getElementById('vidFrame').style.backgroundColor='blue';
    }""", [config,track])
    await page.wait_for_timeout(50)
    pixel_info = await page.evaluate("""()=>{
        Object.defineProperty(vizEl, "videoWidth", {configurable:true, value:192});
        Object.defineProperty(vizEl, "videoHeight", {configurable:true, value:6});
        window.__yomiPixelTestRenderViz();
        let box=vizCanvas.getBoundingClientRect();
        return {w:box.width,h:box.height,srcW:vizCanvas.width,srcH:vizCanvas.height};
    }""")
    assert 1 <= pixel_info["srcW"] <= 192*viz_length/8 and pixel_info["srcH"] == 6, pixel_info
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
    # The border must occupy real layout pixels, with no ::after painted
    # over the content. In CEF that layering had allowed art to bleed into it.
    border_info=await page.evaluate("""()=>({
        art:getComputedStyle(artFrame).borderTopWidth,
        vid:getComputedStyle(vidFrame).borderTopWidth,
        overlayContent:getComputedStyle(vidFrame,'::after').content
    })""")
    assert border_info["overlayContent"] in ("none","normal"), border_info
    if border:
        assert float(border_info["art"].removesuffix('px'))>=1, border_info
    image = Image.open(io.BytesIO(await page.screenshot(omit_background=True))).convert("RGBA")
    if with_video and with_art and layout == "Reflow":
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
    # Native spectrum cells remain square even when the requested length is
    # wider than the text viewport. Peak pooling reduces columns when necessary.
    assert abs(boxes["viz"]["w"]/boxes["viz"]["h"]-(pixel_info["srcW"]/pixel_info["srcH"])) < .045, (boxes,pixel_info)
    assert boxes["viz"]["w"] <= 1400, ("Visualizer exceeds OBS viewport",boxes)
    assert boxes["viz"]["x"]+boxes["viz"]["w"] <= 1400.5, boxes
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

async def check_text_match(page):
    # Actual browser layout, not just a reconstructed width formula. This
    # verifies the checkbox, visible glyph measurement, symmetric overhang,
    # 1:1 frequency cells, and track-to-track width changes.
    config = {
        "artwork_enabled": True, "video_enabled": True,
        "title_enabled": True, "channel_enabled": True,
        "visualizer_enabled": True, "visualizer_match_text_overhang": True,
        "visualizer_length_multiplier": 1,
        "media_width": 180, "media_height": 100,
        "overlay_text_gap_px": 14, "text_size": 28,
        "text_font": "Arial", "text_alignment": "Auto",
        "media_border_enabled": False,
    }
    measurements=[]
    for title, channel in [
        ("Short", "Channel"),
        ("Longer title testing that the frequency display follows the title", "Channel"),
        ("Short", "A longer musical channel name that should determine the visualizer width"),
    ]:
        track={"index":1,"title":title,"channel":channel,
               "artwork":"unavailable","full_artwork":"unavailable",
               "visualizer":"unavailable"}
        output=await page.evaluate("""([config,track])=>{
            window.__yomiPixelTestApply(config,track,{});
            const region=document.getElementById('text').getBoundingClientRect(),vr=document.getElementById('viz').getBoundingClientRect();
            const spans=['title','channel'].map(id=>document.getElementById(id)).filter(x=>!x.classList.contains('hidden')).map(el=>{
                const range=document.createRange();range.selectNodeContents(el);
                const rect=range.getBoundingClientRect();
                return {left:Math.max(region.left,rect.left),right:Math.min(region.right,rect.right)};
            });
            return {left:vr.left,right:vr.right,width:vr.width,height:vr.height,
                    nativeColumns:window.__yomiPixelTestColumns(),rows:vizEl.videoHeight||6,
                    textLeft:Math.min(...spans.map(x=>x.left)),
                    textRight:Math.max(...spans.map(x=>x.right)),
                    auto:window.__yomiPixelTestAutoMatch()};
        }""",[config,track])
        assert output["auto"], ("Auto length incorrectly disabled",output)
        assert output["width"] > 0 and output["width"] <= 1400, output
        assert abs(output["width"]/output["nativeColumns"]-output["height"]/6) < 0.05, output
        left_gap=output["textLeft"]-output["left"]
        right_gap=output["right"]-output["textRight"]
        assert left_gap >= 0, output
        # A display cell is indivisible; equal left/right padding is accurate
        # to within one native square-pixel cell.
        cell=output["height"]/6
        assert -cell-2 <= right_gap-left_gap <= 2, (output,left_gap,right_gap)
        measurements.append(output)
    assert measurements[0]["width"] < measurements[1]["width"], measurements
    assert measurements[0]["width"] < measurements[2]["width"], measurements
    print("PASS text-matched OBS geometry: title/channel selection, symmetric overhang, automatic square cells")
    # The very same setting must not hijack the manual length control when off.
    config["visualizer_match_text_overhang"]=False
    config["visualizer_length_multiplier"]=1
    track["title"]="A much longer song title does not affect the manual length"
    short=await page.evaluate("""([c,t])=>{
        window.__yomiPixelTestApply(c,t,{});
        return {w:document.getElementById('viz').getBoundingClientRect().width,auto:window.__yomiPixelTestAutoMatch()};
    }""",[config,track])
    config["visualizer_length_multiplier"]=4
    long=await page.evaluate("""([c,t])=>{
        window.__yomiPixelTestApply(c,t,{});
        return {w:document.getElementById('viz').getBoundingClientRect().width,auto:window.__yomiPixelTestAutoMatch()};
    }""",[config,track])
    assert not short["auto"] and not long["auto"], (short,long)
    assert long["w"] > short["w"], (short,long)
    print("PASS text match off: manual 1.0–8.0 length remains independent")

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
                         (2,"Soft","Reflow",False,16/9,False),
                         (2,"Soft","Reflow",True,16/9,True,2)]:
                await check(page,*args)
            await check_text_match(page)
            # Standalone /visualizer should honor its requested long spectrum
            # while preserving square pixels and staying within the source.
            standalone=await page.evaluate("""()=>{
                window.__yomiPixelTestApply({
                    artwork_enabled:false,video_enabled:false,title_enabled:false,
                    channel_enabled:false,visualizer_enabled:true,
                    visualizer_length_multiplier:4
                },{index:1,visualizer:'unavailable'}, {});
                const r=viz.getBoundingClientRect();
                return {width:r.width,height:r.height,x:r.x};
            }""")
            assert standalone["width"] > 1200 and standalone["width"] <= 1400,standalone
            assert standalone["width"]/standalone["height"] > 15,standalone
            assert abs(standalone["x"])<1,standalone
            print("PASS standalone OBS visualizer honors requested 4.0 length and square pixels")
            assert not errors,errors
        finally:
            await browser.close()

if __name__=="__main__":
    asyncio.run(main())
