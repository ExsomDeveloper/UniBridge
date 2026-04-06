package com.unibridge.share;

import android.app.Activity;
import android.content.ContentValues;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.MediaStore;
import androidx.core.content.FileProvider;
import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.io.OutputStream;

public class UniBridgeSharePlugin {

    public static void shareImage(Activity activity, String imagePath, String text) {
        try {
            Uri imageUri = getImageUri(activity, imagePath);

            Intent intent = new Intent(Intent.ACTION_SEND);
            intent.setType("image/png");
            intent.putExtra(Intent.EXTRA_STREAM, imageUri);
            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            if (text != null && !text.isEmpty())
                intent.putExtra(Intent.EXTRA_TEXT, text);

            activity.startActivity(Intent.createChooser(intent, "Поделиться"));
        } catch (Exception e) {
            android.util.Log.e("UniBridgeShare", "shareImage failed: " + e.getMessage());
            if (text != null && !text.isEmpty())
                shareText(activity, text);
        }
    }

    public static void shareText(Activity activity, String text) {
        Intent intent = new Intent(Intent.ACTION_SEND);
        intent.setType("text/plain");
        intent.putExtra(Intent.EXTRA_TEXT, text);
        activity.startActivity(Intent.createChooser(intent, "Поделиться"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Uri getImageUri(Activity activity, String imagePath) throws Exception {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            // API 29+: MediaStore — no FileProvider or permissions needed
            ContentValues values = new ContentValues();
            values.put(MediaStore.Images.Media.DISPLAY_NAME, "share_" + System.currentTimeMillis() + ".png");
            values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
            Uri uri = activity.getContentResolver()
                              .insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
            if (uri == null) throw new Exception("MediaStore insert returned null");
            try (OutputStream out = activity.getContentResolver().openOutputStream(uri);
                 InputStream  in  = new FileInputStream(imagePath)) {
                byte[] buf = new byte[8192];
                int n;
                while ((n = in.read(buf)) > 0) out.write(buf, 0, n);
            }
            return uri;
        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            // API 24-28: FileProvider (declared in manifest via UniBridgeShareAndroidManifestInjector)
            String authority = activity.getPackageName() + ".unibridgeshare.provider";
            return FileProvider.getUriForFile(activity, authority, new File(imagePath));
        } else {
            // API 21-23: direct file:// URI (no restrictions before Android N)
            return Uri.fromFile(new File(imagePath));
        }
    }
}
