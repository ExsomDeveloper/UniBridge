using System;

namespace StoreScreenshots
{
    [Serializable]
    public class ScreenshotResolution
    {
        public string Label = "1080x1920";
        public int Width = 1080;
        public int Height = 1920;
        public bool Enabled = true;

        public ScreenshotResolution() { }

        public ScreenshotResolution(string label, int width, int height)
        {
            Label = label;
            Width = width;
            Height = height;
            Enabled = true;
        }
    }
}
