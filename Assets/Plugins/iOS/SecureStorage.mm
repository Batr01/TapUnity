#import <Foundation/Foundation.h>
#import <Security/Security.h>

static NSString *const kTapSecureStorageService = @"com.tapbrawl.auth.secure";

static NSDictionary *BuildQuery(NSString *account, NSData *data) {
    NSMutableDictionary *query = [@{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: kTapSecureStorageService,
        (__bridge id)kSecAttrAccount: account,
    } mutableCopy];

    if (data != nil) {
        query[(__bridge id)kSecValueData] = data;
    }

    return query;
}

static OSStatus SetKeychainValue(NSString *account, NSString *value) {
    NSData *data = [value dataUsingEncoding:NSUTF8StringEncoding];
    if (data == nil) {
        return errSecParam;
    }

    NSDictionary *query = BuildQuery(account, nil);
    SecItemDelete((__bridge CFDictionaryRef)query);

    NSDictionary *attributes = BuildQuery(account, data);
    return SecItemAdd((__bridge CFDictionaryRef)attributes, NULL);
}

static NSString *GetKeychainValue(NSString *account) {
    NSMutableDictionary *query = [BuildQuery(account, nil) mutableCopy];
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;

    CFTypeRef result = NULL;
    OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
    if (status != errSecSuccess || result == NULL) {
        return nil;
    }

    NSData *data = (__bridge_transfer NSData *)result;
    return [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
}

static OSStatus DeleteKeychainValue(NSString *account) {
    NSDictionary *query = BuildQuery(account, nil);
    return SecItemDelete((__bridge CFDictionaryRef)query);
}

extern "C" {

void _TapSecureStorageSet(const char *key, const char *value) {
    if (key == NULL) {
        return;
    }

    NSString *account = [NSString stringWithUTF8String:key];
    if (value == NULL || value[0] == '\0') {
        DeleteKeychainValue(account);
        return;
    }

    NSString *payload = [NSString stringWithUTF8String:value];
    SetKeychainValue(account, payload);
}

const char *_TapSecureStorageGet(const char *key) {
    if (key == NULL) {
        return NULL;
    }

    NSString *account = [NSString stringWithUTF8String:key];
    NSString *value = GetKeychainValue(account);
    if (value == nil) {
        return NULL;
    }

    const char *utf8 = [value UTF8String];
    if (utf8 == NULL) {
        return NULL;
    }

    size_t length = strlen(utf8);
    char *buffer = (char *)malloc(length + 1);
    if (buffer == NULL) {
        return NULL;
    }

    memcpy(buffer, utf8, length + 1);
    return buffer;
}

void _TapSecureStorageDelete(const char *key) {
    if (key == NULL) {
        return;
    }

    NSString *account = [NSString stringWithUTF8String:key];
    DeleteKeychainValue(account);
}

bool _TapSecureStorageContainsKey(const char *key) {
    if (key == NULL) {
        return false;
    }

    NSString *account = [NSString stringWithUTF8String:key];
    return GetKeychainValue(account) != nil;
}

void _TapSecureStorageFree(const char *ptr) {
    if (ptr != NULL) {
        free((void *)ptr);
    }
}

}
