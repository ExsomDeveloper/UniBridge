#import <UIKit/UIKit.h>

// Объявление Unity internal функции для получения root view controller
extern UIViewController* UnityGetGLViewController(void);

// Тип колбэка для передачи результата обратно в C#
// resultCode: 0 = Completed, 1 = Cancelled
typedef void (*UniBridgeShareCallback)(int resultCode);

// ── Вспомогательная функция настройки iPad popover ────────────────────────

static void ConfigurePopover(UIActivityViewController* vc, UIViewController* root)
{
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad)
    {
        vc.popoverPresentationController.sourceView = root.view;
        vc.popoverPresentationController.sourceRect =
            CGRectMake(root.view.bounds.size.width / 2.0,
                       root.view.bounds.size.height / 2.0, 1, 1);
    }
}

// ── Вспомогательная функция показа UIActivityViewController ───────────────

static void PresentShareSheet(NSArray* items, UniBridgeShareCallback callback)
{
    UIViewController* root = UnityGetGLViewController();
    if (!root) {
        if (callback) callback(1); // Cancelled — нет контроллера
        return;
    }

    UIActivityViewController* vc = [[UIActivityViewController alloc]
        initWithActivityItems:items applicationActivities:nil];

    // Захват результата: completionWithItemsHandler вызывается на main thread
    // после того как пользователь завершил или закрыл диалог
    vc.completionWithItemsHandler = ^(UIActivityType activityType,
                                       BOOL completed,
                                       NSArray* returnedItems,
                                       NSError* activityError) {
        if (callback) callback(completed ? 0 : 1);
    };

    ConfigurePopover(vc, root);
    [root presentViewController:vc animated:YES completion:nil];
}

extern "C"
{
    void _UniBridgeShare_Text(const char* text, UniBridgeShareCallback callback)
    {
        if (!text) {
            if (callback) callback(1);
            return;
        }

        NSString* textStr = [NSString stringWithUTF8String:text];
        PresentShareSheet(@[textStr], callback);
    }

    void _UniBridgeShare_Image(const char* imagePath, UniBridgeShareCallback callback)
    {
        if (!imagePath) {
            if (callback) callback(1);
            return;
        }

        NSString* path  = [NSString stringWithUTF8String:imagePath];
        UIImage*  image = [UIImage imageWithContentsOfFile:path];
        if (!image) {
            if (callback) callback(1);
            return;
        }

        PresentShareSheet(@[image], callback);
    }

    void _UniBridgeShare_TextAndImage(const char* text, const char* imagePath, UniBridgeShareCallback callback)
    {
        NSMutableArray* items = [NSMutableArray array];

        if (text)
        {
            NSString* textStr = [NSString stringWithUTF8String:text];
            [items addObject:textStr];
        }

        if (imagePath)
        {
            NSString* path  = [NSString stringWithUTF8String:imagePath];
            UIImage*  image = [UIImage imageWithContentsOfFile:path];
            if (image) [items addObject:image];
        }

        if (items.count == 0) {
            if (callback) callback(1);
            return;
        }

        PresentShareSheet(items, callback);
    }
}
