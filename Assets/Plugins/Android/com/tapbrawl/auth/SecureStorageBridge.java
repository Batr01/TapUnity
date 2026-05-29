package com.tapbrawl.auth;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;

import com.unity3d.player.UnityPlayer;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public final class SecureStorageBridge {
    private static final String PREFS_NAME = "tb_secure_store";
    private static final String KEYSTORE_ALIAS = "tap_brawl_auth_key";
    private static final String ANDROID_KEYSTORE = "AndroidKeyStore";
    private static final int GCM_TAG_BITS = 128;
    private static final int IV_BYTES = 12;

    private SecureStorageBridge() {
    }

    public static void setString(String key, String value) {
        if (key == null) {
            return;
        }

        if (value == null || value.isEmpty()) {
            deleteKey(key);
            return;
        }

        try {
            byte[] iv = new byte[IV_BYTES];
            new java.security.SecureRandom().nextBytes(iv);

            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            cipher.init(Cipher.ENCRYPT_MODE, getOrCreateSecretKey(), new GCMParameterSpec(GCM_TAG_BITS, iv));
            byte[] encrypted = cipher.doFinal(value.getBytes(StandardCharsets.UTF_8));

            ByteBuffer buffer = ByteBuffer.allocate(iv.length + encrypted.length);
            buffer.put(iv);
            buffer.put(encrypted);

            prefs().edit().putString(key, Base64.encodeToString(buffer.array(), Base64.NO_WRAP)).apply();
        } catch (Throwable t) {
            throw new RuntimeException("SecureStorageBridge.setString failed for key=" + key, t);
        }
    }

    public static String getString(String key, String defaultValue) {
        if (key == null) {
            return defaultValue;
        }

        String stored = prefs().getString(key, null);
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
            return defaultValue;
        }
    }

    public static void deleteKey(String key) {
        if (key == null) {
            return;
        }
        prefs().edit().remove(key).apply();
    }

    public static boolean containsKey(String key) {
        if (key == null) {
            return false;
        }
        return prefs().contains(key);
    }

    private static SharedPreferences prefs() {
        Context context = UnityPlayer.currentActivity.getApplicationContext();
        return context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
    }

    private static SecretKey getOrCreateSecretKey() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(ANDROID_KEYSTORE);
        keyStore.load(null);

        if (!keyStore.containsAlias(KEYSTORE_ALIAS)) {
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
        }

        return ((KeyStore.SecretKeyEntry) keyStore.getEntry(KEYSTORE_ALIAS, null)).getSecretKey();
    }
}
