package com.tapbrawl.auth;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.content.Intent;
import android.os.Bundle;
import android.text.TextUtils;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;

public final class GoogleSignInBridge {
    private static final String FRAGMENT_TAG = "TapBrawlGoogleSignInFragment";

    private GoogleSignInBridge() {
    }

    public static void signIn(
            final String webClientId,
            final String callbackGameObject,
            final String successMethod,
            final String errorMethod
    ) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            sendError(callbackGameObject, errorMethod, "Unity activity is null.");
            return;
        }

        activity.runOnUiThread(() -> {
            if (TextUtils.isEmpty(webClientId)) {
                sendError(callbackGameObject, errorMethod, "Google Web Client Id is empty.");
                return;
            }

            try {
                FragmentManager fm = activity.getFragmentManager();
                SignInFragment fragment = (SignInFragment) fm.findFragmentByTag(FRAGMENT_TAG);
                if (fragment == null) {
                    fragment = new SignInFragment();
                    fm.beginTransaction().add(fragment, FRAGMENT_TAG).commitAllowingStateLoss();
                    fm.executePendingTransactions();
                }

                fragment.begin(
                        webClientId,
                        callbackGameObject,
                        successMethod,
                        errorMethod
                );
            } catch (Throwable t) {
                sendError(callbackGameObject, errorMethod, getMessage(t));
            }
        });
    }

    private static void sendSuccess(String gameObject, String method, String idToken) {
        UnityPlayer.UnitySendMessage(gameObject, method, idToken == null ? "" : idToken);
    }

    private static void sendError(String gameObject, String method, String error) {
        UnityPlayer.UnitySendMessage(gameObject, method, error == null ? "Unknown Google Sign-In error." : error);
    }

    private static String getMessage(Throwable t) {
        if (t == null || t.getMessage() == null) {
            return "Unknown Google Sign-In error.";
        }
        return t.getMessage();
    }

    public static final class SignInFragment extends Fragment {
        private static final int RC_SIGN_IN = 91007;

        private String callbackGameObject;
        private String successMethod;
        private String errorMethod;

        public void begin(
                String webClientId,
                String callbackGameObject,
                String successMethod,
                String errorMethod
        ) {
            this.callbackGameObject = callbackGameObject;
            this.successMethod = successMethod;
            this.errorMethod = errorMethod;

            Activity activity = getActivity();
            if (activity == null) {
                sendError(callbackGameObject, errorMethod, "Activity is null.");
                detach();
                return;
            }

            try {
                GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                        .requestEmail()
                        .requestIdToken(webClientId)
                        .build();

                GoogleSignInClient client = GoogleSignIn.getClient(activity, gso);
                client.signOut().addOnCompleteListener(activity, task -> {
                    try {
                        startActivityForResult(client.getSignInIntent(), RC_SIGN_IN);
                    } catch (Throwable t) {
                        sendError(this.callbackGameObject, this.errorMethod, getMessage(t));
                        detach();
                    }
                });
            } catch (Throwable t) {
                sendError(this.callbackGameObject, this.errorMethod, getMessage(t));
                detach();
            }
        }

        @Override
        public void onActivityResult(int requestCode, int resultCode, Intent data) {
            super.onActivityResult(requestCode, resultCode, data);
            if (requestCode != RC_SIGN_IN) {
                return;
            }

            try {
                Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
                GoogleSignInAccount account = task.getResult(ApiException.class);
                String idToken = account == null ? null : account.getIdToken();
                if (TextUtils.isEmpty(idToken)) {
                    sendError(callbackGameObject, errorMethod, "id_token is empty.");
                } else {
                    sendSuccess(callbackGameObject, successMethod, idToken);
                }
            } catch (ApiException ex) {
                sendError(callbackGameObject, errorMethod, "ApiException code=" + ex.getStatusCode() + " " + getMessage(ex));
            } catch (Throwable t) {
                sendError(callbackGameObject, errorMethod, getMessage(t));
            } finally {
                detach();
            }
        }

        private void detach() {
            try {
                Activity activity = getActivity();
                if (activity == null) {
                    return;
                }
                FragmentManager fm = activity.getFragmentManager();
                fm.beginTransaction().remove(this).commitAllowingStateLoss();
            } catch (Throwable ignored) {
                // no-op
            }
        }
    }
}
