package com.tapbrawl.auth;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public final class SecureStorageBridge {
    private static final String TAG = "SecureStorageBridge";
    private static final String PREFS_NAME = "tb_secure_store";
    private static final String KEYSTORE_ALIAS = "tap_brawl_auth_key";
    private static final String ANDROID_KEYSTORE = "AndroidKeyStore";
    private static final int GCM_TAG_BITS = 128;
    private static final int IV_BYTES = 12;

    private SecureStorageBridge() {
    }

    public static boolean setString(String key, String value) {
        if (key == null) {
            return true;
        }

        if (value == null || value.isEmpty()) {
            deleteKey(key);
            return true;
        }

        if (setStringInternal(key, value)) {
            return true;
        }

        Log.w(TAG, "setString failed, resetting secure store. key=" + key);
        resetSecureStore();
        return setStringInternal(key, value);
    }

    public static String getString(String key, String defaultValue) {
        if (key == null) {
            return defaultValue;
        }

        SharedPreferences preferences = prefsOrNull();
        if (preferences == null) {
            return defaultValue;
        }

        String stored = preferences.getString(key, null);
        if (stored == null || stored.isEmpty()) {
            return defaultValue;
        }

        try {
            byte[] payload = Base64.decode(stored, Base64.NO_WRAP);
            if (payload.length <= IV_BYTES) {
                return defaultValue;
            }

            ByteBuffer buffer = ByteBuffer.wrap(payload);
            byte[] iv = new byte[IV_BYTES];
            buffer.get(iv);
            byte[] encrypted = new byte[buffer.remaining()];
            buffer.get(encrypted);

            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            cipher.init(Cipher.DECRYPT_MODE, getOrCreateSecretKey(), new GCMParameterSpec(GCM_TAG_BITS, iv));
            byte[] plain = cipher.doFinal(encrypted);
            return new String(plain, StandardCharsets.UTF_8);
        } catch (Throwable t) {
            Log.w(TAG, "getString failed for key=" + key, t);
            return defaultValue;
        }
    }

    public static void deleteKey(String key) {
        if (key == null) {
            return;
        }

        SharedPreferences preferences = prefsOrNull();
        if (preferences == null) {
            return;
        }

        preferences.edit().remove(key).apply();
    }

    public static boolean containsKey(String key) {
        if (key == null) {
            return false;
        }

        SharedPreferences preferences = prefsOrNull();
        return preferences != null && preferences.contains(key);
    }

    private static boolean setStringInternal(String key, String value) {
        try {
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            // Android Keystore генерирует IV сам — передача своего IV запрещена (API 31+).
            cipher.init(Cipher.ENCRYPT_MODE, getOrCreateSecretKey());
            byte[] iv = cipher.getIV();
            byte[] encrypted = cipher.doFinal(value.getBytes(StandardCharsets.UTF_8));

            ByteBuffer buffer = ByteBuffer.allocate(iv.length + encrypted.length);
            buffer.put(iv);
            buffer.put(encrypted);

            SharedPreferences preferences = prefsOrNull();
            if (preferences == null) {
                return false;
            }

            preferences.edit().putString(key, Base64.encodeToString(buffer.array(), Base64.NO_WRAP)).apply();
            return true;
        } catch (Throwable t) {
            Log.e(TAG, "setString failed for key=" + key, t);
            return false;
        }
    }

    private static void resetSecureStore() {
        try {
            KeyStore keyStore = KeyStore.getInstance(ANDROID_KEYSTORE);
            keyStore.load(null);
            if (keyStore.containsAlias(KEYSTORE_ALIAS)) {
                keyStore.deleteEntry(KEYSTORE_ALIAS);
            }
        } catch (Throwable t) {
            Log.e(TAG, "reset keystore alias failed", t);
        }

        try {
            SharedPreferences preferences = prefsOrNull();
            if (preferences != null) {
                preferences.edit().clear().apply();
            }
        } catch (Throwable t) {
            Log.e(TAG, "clear secure prefs failed", t);
        }
    }

    private static SharedPreferences prefsOrNull() {
        Context context = getContext();
        if (context == null) {
            Log.e(TAG, "Application context unavailable");
            return null;
        }
        return context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
    }

    private static Context getContext() {
        if (UnityPlayer.currentActivity != null) {
            return UnityPlayer.currentActivity.getApplicationContext();
        }

        try {
            Object context = UnityPlayer.class.getField("currentContext").get(null);
            if (context instanceof Context) {
                return ((Context) context).getApplicationContext();
            }
        } catch (Throwable ignored) {
        }

        return null;
    }

    private static SecretKey getOrCreateSecretKey() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(ANDROID_KEYSTORE);
        keyStore.load(null);

        if (keyStore.containsAlias(KEYSTORE_ALIAS)) {
            try {
                return ((KeyStore.SecretKeyEntry) keyStore.getEntry(KEYSTORE_ALIAS, null)).getSecretKey();
            } catch (Exception e) {
                Log.w(TAG, "Keystore alias invalid, recreating", e);
                keyStore.deleteEntry(KEYSTORE_ALIAS);
            }
        }

        KeyGenerator keyGenerator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEYSTORE);
        KeyGenParameterSpec spec = new KeyGenParameterSpec.Builder(
                KEYSTORE_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT
        )
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)
                .build();
        keyGenerator.init(spec);
        keyGenerator.generateKey();

        return ((KeyStore.SecretKeyEntry) keyStore.getEntry(KEYSTORE_ALIAS, null)).getSecretKey();
    }
}
