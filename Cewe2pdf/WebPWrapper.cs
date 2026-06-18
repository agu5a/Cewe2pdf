using SkiaSharp;

namespace WebPWrapper {
    // Original Windows-only WebP wrapper replaced by SkiaSharp which handles WebP natively on all platforms.
    class WebP {
        public SKBitmap Load(string pathFileName) {
            return SKBitmap.Decode(pathFileName);
        }
    }
}
