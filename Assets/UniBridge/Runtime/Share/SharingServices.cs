using System;
using System.Collections;
using UnityEngine;

namespace UniBridge
{
    /// <summary>
    /// Public API for sharing content to social networks.
    /// Supports text, images, and screen screenshots.
    /// Controlled by the UNIBRIDGESHARE_ENABLED define.
    /// </summary>
    public static class SharingServices
    {
        public static bool IsSupported   => UniBridgeShare.IsSupported;
        public static bool IsInitialized => UniBridgeShare.IsInitialized;

        /// <summary>
        /// Show the native share dialog with the provided content.
        /// </summary>
        /// <param name="callback">
        /// Called after the operation completes.
        /// <c>result</c> contains <see cref="ShareResultCode"/> — the status on platforms that report it.
        /// <c>error</c> is non-null only on an initialization error or invalid data.
        /// </param>
        /// <param name="items">One or more items to share.
        /// The first text and first image/screenshot are used.</param>
        public static void ShowShareSheet(
            Action<ShareSheetResult, ShareError> callback,
            params ShareItem[] items)
        {
            if (items == null || items.Length == 0)
            {
                callback?.Invoke(null, new ShareError("Не передано ни одного ShareItem"));
                return;
            }

            bool hasScreenshot = HasScreenshot(items);

            if (hasScreenshot)
                ShareCoroutineRunner.Run(CaptureAndShare(items, callback));
            else
                DoShare(BuildShareData(items), callback);
        }

        // ── Private methods ──────────────────────────────────────────────────

        private static bool HasScreenshot(ShareItem[] items)
        {
            foreach (var item in items)
                if (item.Kind == ShareItem.ItemKind.Screenshot)
                    return true;
            return false;
        }

        private static IEnumerator CaptureAndShare(
            ShareItem[] items,
            Action<ShareSheetResult, ShareError> callback)
        {
            yield return new WaitForEndOfFrame();

            var texture = ScreenCapture.CaptureScreenshotAsTexture();

            // Replace the Screenshot item with an Image containing the captured texture
            var resolved = ReplaceScreenshot(items, texture);

            DoShare(BuildShareData(resolved), callback);

            // Texture is no longer needed after handing it off to the adapter
            // (the adapter saves the PNG before calling the Intent / native plugin)
            UnityEngine.Object.Destroy(texture);
        }

        private static ShareItem[] ReplaceScreenshot(ShareItem[] items, Texture2D screenshot)
        {
            var result = new ShareItem[items.Length];
            bool replaced = false;

            for (int i = 0; i < items.Length; i++)
            {
                if (!replaced && items[i].Kind == ShareItem.ItemKind.Screenshot)
                {
                    result[i] = ShareItem.Image(screenshot);
                    replaced  = true;
                }
                else
                {
                    result[i] = items[i];
                }
            }

            return result;
        }

        private static ShareData BuildShareData(ShareItem[] items)
        {
            string   text     = null;
            Texture2D image   = null;
            string   imageUrl = null;

            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case ShareItem.ItemKind.Text     when text     == null: text     = item.TextValue;     break;
                    case ShareItem.ItemKind.Image    when image    == null: image    = item.ImageValue;    break;
                    case ShareItem.ItemKind.ImageUrl when imageUrl == null: imageUrl = item.ImageUrlValue; break;
                }
            }

            if (text != null && image != null)    return ShareData.WithTextAndImage(text, image);
            if (text != null && imageUrl != null) return ShareData.WithTextAndImageUrl(text, imageUrl);
            if (image    != null)                 return ShareData.WithImage(image);
            if (imageUrl != null)                 return ShareData.WithImageUrl(imageUrl);
            return ShareData.WithText(text ?? string.Empty);
        }

        private static void DoShare(ShareData data, Action<ShareSheetResult, ShareError> callback)
        {
            UniBridgeShare.Share(data, callback);
        }
    }
}
