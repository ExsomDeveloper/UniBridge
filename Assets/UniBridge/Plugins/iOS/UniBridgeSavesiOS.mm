#import <Foundation/Foundation.h>

// ── Helpers ──────────────────────────────────────────────────────────────────

static char* MakeCString(NSString* str)
{
    if (str == nil) return NULL;
    const char* cstr = [str UTF8String];
    if (cstr == NULL) return NULL;
    size_t len = strlen(cstr) + 1;
    char* result = (char*)malloc(len);
    memcpy(result, cstr, len);
    return result;
}

// ── Exported functions ────────────────────────────────────────────────────────

extern "C"
{
    /// Returns true if NSUbiquitousKeyValueStore is available
    /// (iCloud account signed in and KV storage entitlement present).
    bool UniBridgeSaves_IsAvailable()
    {
        NSUbiquitousKeyValueStore* store = [NSUbiquitousKeyValueStore defaultStore];
        if (store == nil) return false;
        // Trigger initial sync; returns NO if entitlement is missing
        return [store synchronize];
    }

    /// Returns the stored string for key, or empty string if not found.
    /// Caller must free the returned pointer.
    const char* UniBridgeSaves_GetString(const char* key)
    {
        NSString* nsKey = [NSString stringWithUTF8String:key];
        NSString* value = [[NSUbiquitousKeyValueStore defaultStore] stringForKey:nsKey];
        if (value == nil) return MakeCString(@"");
        return MakeCString(value);
    }

    /// Stores a string value for the given key.
    void UniBridgeSaves_SetString(const char* key, const char* value)
    {
        NSString* nsKey   = [NSString stringWithUTF8String:key];
        NSString* nsValue = [NSString stringWithUTF8String:value];
        [[NSUbiquitousKeyValueStore defaultStore] setString:nsValue forKey:nsKey];
    }

    /// Removes the value for the given key.
    void UniBridgeSaves_Remove(const char* key)
    {
        NSString* nsKey = [NSString stringWithUTF8String:key];
        [[NSUbiquitousKeyValueStore defaultStore] removeObjectForKey:nsKey];
    }

    /// Returns true if the key has a stored value.
    bool UniBridgeSaves_HasKey(const char* key)
    {
        NSString* nsKey = [NSString stringWithUTF8String:key];
        return [[NSUbiquitousKeyValueStore defaultStore] objectForKey:nsKey] != nil;
    }

    /// Explicitly synchronizes local changes with iCloud.
    void UniBridgeSaves_Synchronize()
    {
        [[NSUbiquitousKeyValueStore defaultStore] synchronize];
    }
}
