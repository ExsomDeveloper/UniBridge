using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// A unit of content for sharing. Passed to <see cref="SharingServices.ShowShareSheet"/>.
    /// When multiple items are provided, the first text and first image are used.
    /// </summary>
    public class ShareItem
    {
        public enum ItemKind { Text, Image, ImageUrl, Screenshot }

        public ItemKind  Kind          { get; }
        public string    TextValue     { get; }
        public Texture2D ImageValue    { get; }
        public string    ImageUrlValue { get; }

        private ShareItem(ItemKind kind, string text = null, Texture2D image = null, string imageUrl = null)
        {
            Kind          = kind;
            TextValue     = text;
            ImageValue    = image;
            ImageUrlValue = imageUrl;
        }

        /// <summary>Share text.</summary>
        public static ShareItem Text(string text)
            => new ShareItem(ItemKind.Text, text: text);

        /// <summary>Share an image (Texture2D). Supported on Android and iOS.</summary>
        public static ShareItem Image(Texture2D image)
            => new ShareItem(ItemKind.Image, image: image);

        /// <summary>Share an image by URL. Supported on Playgama (WebGL).</summary>
        public static ShareItem ImageUrl(string url)
            => new ShareItem(ItemKind.ImageUrl, imageUrl: url);

        /// <summary>
        /// Capture a screenshot and share it. The capture happens at the end of the current frame
        /// (<c>WaitForEndOfFrame</c>) before the dialog is shown.
        /// </summary>
        public static ShareItem Screenshot()
            => new ShareItem(ItemKind.Screenshot);
    }
}
