package in.surakshaxr.storage;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Device-local at-rest wrapping. Never logs the seed or encrypted configuration. */
public final class IssuerVault {
    private static final String ALIAS = "surakshaxr.demo.issuer.wrap.v1";
    private final SharedPreferences preferences;
    public IssuerVault(Context context) { preferences = context.getSharedPreferences("surakshaxr_issuer", Context.MODE_PRIVATE); }
    private SecretKey key() throws Exception {
        KeyStore store = KeyStore.getInstance("AndroidKeyStore"); store.load(null);
        if (!store.containsAlias(ALIAS)) {
            // Never regenerate a missing key over existing ciphertext.
            if (preferences.contains("seed")) throw new IllegalStateException("Issuer key unavailable; preserved for recovery");
            KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
            generator.init(new KeyGenParameterSpec.Builder(ALIAS, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).setRandomizedEncryptionRequired(true).build());
            generator.generateKey();
        }
        return (SecretKey)store.getKey(ALIAS, null);
    }
    public String read() throws Exception {
        String stored = preferences.getString("seed", null);
        if (stored == null) return null;
        byte[] encrypted = Base64.decode(stored, Base64.NO_WRAP);
        byte[] nonce = Base64.decode(preferences.getString("nonce", ""), Base64.NO_WRAP);
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding"); cipher.init(Cipher.DECRYPT_MODE, key(), new GCMParameterSpec(128, nonce));
        byte[] seed = cipher.doFinal(encrypted);
        try { return Base64.encodeToString(seed, Base64.NO_WRAP); } finally { java.util.Arrays.fill(seed, (byte)0); }
    }
    public void write(String seedB64) throws Exception {
        if (preferences.contains("seed")) throw new IllegalStateException("Issuer already provisioned");
        byte[] seed = Base64.decode(seedB64, Base64.NO_WRAP);
        try {
            if (seed.length != 32) throw new IllegalArgumentException("Invalid issuer seed");
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding"); cipher.init(Cipher.ENCRYPT_MODE, key());
            byte[] encrypted = cipher.doFinal(seed);
            if (!preferences.edit().putString("seed", Base64.encodeToString(encrypted, Base64.NO_WRAP)).putString("nonce", Base64.encodeToString(cipher.getIV(), Base64.NO_WRAP)).commit())
                throw new IllegalStateException("Issuer persistence failed");
        } finally { java.util.Arrays.fill(seed, (byte)0); }
    }
}
