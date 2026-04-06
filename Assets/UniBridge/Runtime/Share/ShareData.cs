using UnityEngine;

namespace UniBridge
{
    public class ShareData
    {
        public string   Text     { get; }
        public Texture2D Image   { get; }   // Android/iOS: сохраняется во временный файл
        public string   ImageUrl { get; }   // Playgama/web: URL изображения

        private ShareData(string text, Texture2D image, string imageUrl)
        {
            Text     = text;
            Image    = image;
            ImageUrl = imageUrl;
        }

        public static ShareData WithText(string text)
            => new ShareData(text, null, null);

        public static ShareData WithImage(Texture2D image)
            => new ShareData(null, image, null);

        public static ShareData WithImageUrl(string url)
            => new ShareData(null, null, url);

        public static ShareData WithTextAndImage(string text, Texture2D image)
            => new ShareData(text, image, null);

        public static ShareData WithTextAndImageUrl(string text, string imageUrl)
            => new ShareData(text, null, imageUrl);
    }
}
