#!/usr/bin/env python3
"""Repair physically square local WPF visualizer pixels in the large controller."""
from pathlib import Path
p=Path("payload/app/YomiControllerWpf.cs")
s=p.read_text(encoding="utf-8-sig")
old=s
def patch(a,b):
    global s
    n=s.count(a)
    if n!=1:
        if n==0 and b in s:return
        raise RuntimeError("Expected one WPF source anchor, found "+str(n)+": "+a[:140])
    s=s.replace(a,b)

patch("""        private readonly WriteableBitmap _bitmap;
        private readonly object _clockSync""", """        private readonly WriteableBitmap _bitmap;
        // The local controller background is a flexible rectangle. Stretch.Fill
        // on a 192x8 spectrum made wildly rectangular pixels. Peak-pool ALL
        // source frequency columns to a square physical-pixel display grid.
        private readonly bool _squarePixelVisualizer;
        private WriteableBitmap _squarePixelBitmap;
        private byte[] _squarePixelFrame;
        private FrameworkElement _squareViewport;
        private Image _squareBackdrop;
        private SizeChangedEventHandler _squareViewportChanged;
        private readonly object _clockSync""")
patch("""            _target = target;
            _width = width;""","""            _target = target;
            _squarePixelVisualizer = String.Equals(target.Name, "VisualizerFrameHost", StringComparison.Ordinal);
            _width = width;""")
marker="""        private void RecordPresentationCadence(long now)
        {"""
method="""        private bool PaintSquareVisualizer()
        {
            if (!_squarePixelVisualizer) return false;
            if (_squareViewport == null)
            {
                DependencyObject parent = _target;
                for (int i = 0; i < 10 && parent != null; i++)
                {
                    FrameworkElement candidate = parent as FrameworkElement;
                    if (candidate != null && candidate.Name == "VisualizerSurface")
                    {
                        _squareViewport = candidate;
                        break;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
                Window window = Window.GetWindow(_target);
                if (window != null) _squareBackdrop = window.FindName("VisualizerBackdropImage") as Image;
                if (_squareViewport != null)
                {
                    _squareViewportChanged = delegate(object sender, SizeChangedEventArgs args)
                    {
                        // Layout changes must update a paused last frame too.
                        if (_deliveredFirstFrame && !_stopping)
                        {
                            try { PaintSquareVisualizer(); } catch { }
                        }
                    };
                    _squareViewport.SizeChanged += _squareViewportChanged;
                }
            }
            if (_squareViewport == null || _squareViewport.ActualWidth < 1 || _squareViewport.ActualHeight < 1)
                return false;

            PresentationSource presentation = PresentationSource.FromVisual(_target);
            double sx = 1.0, sy = 1.0;
            if (presentation != null && presentation.CompositionTarget != null)
            {
                Matrix dpi = presentation.CompositionTarget.TransformToDevice;
                if (dpi.M11 > 0.25 && dpi.M11 < 5) sx = dpi.M11;
                if (dpi.M22 > 0.25 && dpi.M22 < 5) sy = dpi.M22;
            }
            int physicalCell = Math.Max(1, (int)Math.Floor(_squareViewport.ActualHeight * sy / Math.Max(1, _height)));
            int columns = Math.Max(1, Math.Min(_width, (int)Math.Floor(_squareViewport.ActualWidth * sx / physicalCell)));
            int displayStride = columns * 4;
            if (_squarePixelBitmap == null || _squarePixelBitmap.PixelWidth != columns || _squarePixelBitmap.PixelHeight != _height)
            {
                _squarePixelBitmap = new WriteableBitmap(columns, _height, 96, 96, PixelFormats.Bgra32, null);
                _squarePixelFrame = new byte[columns * _height * 4];
                _target.Source = _squarePixelBitmap;
            }
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int lo = (int)((long)x * _width / columns);
                    int hi = Math.Min(_width, (int)Math.Ceiling((x + 1) * (double)_width / columns));
                    int bestIndex = (y * _width + lo) * 4;
                    int bestBrightness = -1;
                    for (int fx = lo; fx < hi; fx++)
                    {
                        int src = (y * _width + fx) * 4;
                        int intensity = Math.Max(_presentFrame[src], Math.Max(_presentFrame[src + 1], _presentFrame[src + 2]));
                        if (intensity > bestBrightness) { bestBrightness = intensity; bestIndex = src; }
                    }
                    int dst = y * displayStride + x * 4;
                    Buffer.BlockCopy(_presentFrame, bestIndex, _squarePixelFrame, dst, 4);
                }
            }
            _squarePixelBitmap.WritePixels(new Int32Rect(0, 0, columns, _height), _squarePixelFrame, displayStride, 0);
            if (_squareBackdrop != null)
            {
                // Canvas has zero desired size; changing this image's dimensions
                // cannot resize the controller, queue, transport, or media frame.
                _squareBackdrop.Width = columns * physicalCell / sx;
                _squareBackdrop.Height = _height * physicalCell / sy;
                Canvas.SetLeft(_squareBackdrop, 0.0);
                Canvas.SetTop(_squareBackdrop, Math.Max(0, (_squareViewport.ActualHeight - _squareBackdrop.Height) / 2));
            }
            return true;
        }

"""
patch(marker,method+marker)
patch("""                _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _presentFrame, stride, 0);
                RecordPresentationCadence(now);
                if (!_deliveredFirstFrame)
                {
                    _target.Source = _bitmap;""", """                bool squareRendered = PaintSquareVisualizer();
                if (!squareRendered) _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _presentFrame, stride, 0);
                RecordPresentationCadence(now);
                if (!_deliveredFirstFrame)
                {
                    _target.Source = squareRendered ? (ImageSource)_squarePixelBitmap : _bitmap;""")
patch("""                    if (_renderHandler != null)
                    {
                        try { CompositionTarget.Rendering -= _renderHandler; } catch { }
                        _renderHandler = null;
                    }
                };
                if (_dispatcher.CheckAccess()) unsubscribe();""", """                    if (_renderHandler != null)
                    {
                        try { CompositionTarget.Rendering -= _renderHandler; } catch { }
                        _renderHandler = null;
                    }
                    if (_squareViewport != null && _squareViewportChanged != null)
                    {
                        try { _squareViewport.SizeChanged -= _squareViewportChanged; } catch { }
                        _squareViewportChanged = null;
                    }
                };
                if (_dispatcher.CheckAccess()) unsubscribe();""")
if s != old:
    p.write_text(s,encoding="utf-8",newline="")
    print("PASS: C# local controller preview now peak-pools to physically square pixels")
else:
    print("PASS: C# local square-pixel renderer was already applied")
assert "private bool PaintSquareVisualizer()" in s
assert "bool squareRendered = PaintSquareVisualizer();" in s
assert "columns * physicalCell / sx" in s
