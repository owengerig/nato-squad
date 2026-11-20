// OG Software shared game services: Google Play Games Services v2 side (see Assets/OGPlatform/OGPlatform.cs).
// The Play Games dependency, App ID and keep rule are added to the Gradle project by
// Assets/OGPlatform/Editor/OGPlatformBuild.cs. Results go back through Callback.onResult(op, ok, data).
package com.ogsoftware.platform;

import android.app.Activity;
import android.content.Intent;

import com.google.android.gms.games.AnnotatedData;
import com.google.android.gms.games.LeaderboardsClient;
import com.google.android.gms.games.PlayGames;
import com.google.android.gms.games.PlayGamesSdk;
import com.google.android.gms.games.SnapshotsClient;
import com.google.android.gms.games.leaderboard.LeaderboardScore;
import com.google.android.gms.games.leaderboard.LeaderboardScoreBuffer;
import com.google.android.gms.games.leaderboard.LeaderboardVariant;
import com.google.android.gms.games.snapshot.Snapshot;
import com.google.android.gms.games.snapshot.SnapshotMetadataChange;
import com.google.android.gms.tasks.Task;

import java.nio.charset.StandardCharsets;

public final class OGPlayGames {
    public interface Callback {
        void onResult(String op, boolean ok, String data);
    }

    private static final int RC_UI = 9071;
    private static Activity activity;
    private static Callback callback;
    private static String playerId = "";

    private OGPlayGames() { }

    private static void send(String op, boolean ok, String data) {
        Callback cb = callback;
        if (cb == null) return;
        try { cb.onResult(op, ok, data == null ? "" : data); } catch (Throwable ignored) { }
    }

    private static String error(Task<?> t) {
        Exception e = t.getException();
        return e == null ? "failed" : String.valueOf(e.getMessage());
    }

    private static String clean(String s) {
        return s == null ? "" : s.replace('\t', ' ').replace('\n', ' ');
    }

    // ------------------------------------------------------------------ sign-in

    public static void init(Activity a, Callback cb) {
        activity = a;
        callback = cb;
        try {
            PlayGamesSdk.initialize(a.getApplicationContext());
        } catch (Throwable t) {
            send("auth", false, "Play Games unavailable: " + t.getMessage());
            return;
        }
        // Play Games signs the player in automatically when they have done so before
        PlayGames.getGamesSignInClient(a).isAuthenticated().addOnCompleteListener(t ->
            onAuth(t.isSuccessful() && t.getResult() != null && t.getResult().isAuthenticated(), t));
    }

    public static void signIn() {
        if (activity == null) return;
        PlayGames.getGamesSignInClient(activity).signIn().addOnCompleteListener(t ->
            onAuth(t.isSuccessful() && t.getResult() != null && t.getResult().isAuthenticated(), t));
    }

    private static void onAuth(boolean ok, Task<?> task) {
        if (!ok) {
            playerId = "";
            send("auth", false, task.isSuccessful() ? "" : error(task));
            return;
        }
        PlayGames.getPlayersClient(activity).getCurrentPlayer().addOnCompleteListener(t -> {
            if (t.isSuccessful() && t.getResult() != null) {
                playerId = t.getResult().getPlayerId();
                send("auth", true, playerId + "\n" + clean(t.getResult().getDisplayName()));
            } else {
                send("auth", true, "\n");
            }
        });
    }

    // ------------------------------------------------------------------ achievements

    public static void reportAchievement(String key, String id, int current, int steps) {
        if (activity == null) return;
        final String op = "ach:" + key;
        final String sent = String.valueOf(current);
        if (steps > 1) {
            if (current <= 0) return;
            PlayGames.getAchievementsClient(activity).setStepsImmediate(id, current).addOnCompleteListener(t ->
                send(op, t.isSuccessful(), t.isSuccessful() ? sent : error(t)));
        } else if (current >= 1) {
            PlayGames.getAchievementsClient(activity).unlockImmediate(id).addOnCompleteListener(t ->
                send(op, t.isSuccessful(), t.isSuccessful() ? sent : error(t)));
        }
    }

    public static void showAchievements() {
        if (activity == null) return;
        PlayGames.getAchievementsClient(activity).getAchievementsIntent().addOnSuccessListener(OGPlayGames::show);
    }

    // ------------------------------------------------------------------ leaderboards

    public static void submitScore(String key, String id, long score) {
        if (activity == null) return;
        final String op = "lb:" + key;
        final String sent = String.valueOf(score);
        PlayGames.getLeaderboardsClient(activity).submitScoreImmediate(id, score).addOnCompleteListener(t ->
            send(op, t.isSuccessful(), t.isSuccessful() ? sent : error(t)));
    }

    public static void loadTopScores(String request, String id, int count) {
        if (activity == null) return;
        int n = Math.max(1, Math.min(25, count)); // Play Games returns at most 25 per page
        PlayGames.getLeaderboardsClient(activity)
            .loadTopScores(id, LeaderboardVariant.TIME_SPAN_ALL_TIME, LeaderboardVariant.COLLECTION_PUBLIC, n)
            .addOnCompleteListener(t -> {
                if (!t.isSuccessful() || t.getResult() == null) { send(request, false, error(t)); return; }
                AnnotatedData<LeaderboardsClient.LeaderboardScores> result = t.getResult();
                LeaderboardsClient.LeaderboardScores scores = result.get();
                StringBuilder rows = new StringBuilder();
                if (scores != null) {
                    LeaderboardScoreBuffer buffer = scores.getScores();
                    try {
                        for (LeaderboardScore s : buffer) {
                            boolean me = s.getScoreHolder() != null && playerId.equals(s.getScoreHolder().getPlayerId());
                            rows.append(s.getRank()).append('\t').append(clean(s.getScoreHolderDisplayName())).append('\t')
                                .append(s.getRawScore()).append('\t').append(me ? '1' : '0').append('\n');
                        }
                    } finally {
                        buffer.release();
                        scores.release();
                    }
                }
                send(request, true, rows.toString());
            });
    }

    public static void showLeaderboard(String id) {
        if (activity == null) return;
        LeaderboardsClient client = PlayGames.getLeaderboardsClient(activity);
        Task<Intent> intent = (id == null || id.isEmpty()) ? client.getAllLeaderboardsIntent() : client.getLeaderboardIntent(id);
        intent.addOnSuccessListener(OGPlayGames::show);
    }

    private static void show(Intent intent) {
        if (activity != null) activity.startActivityForResult(intent, RC_UI);
    }

    // ------------------------------------------------------------------ saved games

    public static void cloudLoad(String slot) {
        if (activity == null) return;
        SnapshotsClient client = PlayGames.getSnapshotsClient(activity);
        client.open(slot, true, SnapshotsClient.RESOLUTION_POLICY_MOST_RECENTLY_MODIFIED).addOnCompleteListener(t -> {
            if (!t.isSuccessful() || t.getResult() == null) { send("load", false, error(t)); return; }
            SnapshotsClient.DataOrConflict<Snapshot> r = t.getResult();
            if (r.isConflict()) {
                // the automatic policy normally resolves these; if not, keep the server's copy and read again
                SnapshotsClient.SnapshotConflict c = r.getConflict();
                client.resolveConflict(c.getConflictId(), c.getSnapshot()).addOnCompleteListener(x -> cloudLoad(slot));
                return;
            }
            Snapshot snapshot = r.getData();
            try {
                byte[] bytes = snapshot.getSnapshotContents().readFully();
                client.discardAndClose(snapshot);
                send("load", true, bytes == null ? "" : new String(bytes, StandardCharsets.UTF_8));
            } catch (Exception e) {
                send("load", false, String.valueOf(e.getMessage()));
            }
        });
    }

    public static void cloudSave(String slot, String data, String description) {
        if (activity == null) return;
        SnapshotsClient client = PlayGames.getSnapshotsClient(activity);
        client.open(slot, true, SnapshotsClient.RESOLUTION_POLICY_MOST_RECENTLY_MODIFIED).addOnCompleteListener(t -> {
            if (!t.isSuccessful() || t.getResult() == null) { send("save", false, error(t)); return; }
            SnapshotsClient.DataOrConflict<Snapshot> r = t.getResult();
            if (r.isConflict()) {
                SnapshotsClient.SnapshotConflict c = r.getConflict();
                client.resolveConflict(c.getConflictId(), c.getSnapshot()).addOnCompleteListener(x -> cloudSave(slot, data, description));
                return;
            }
            Snapshot snapshot = r.getData();
            try {
                snapshot.getSnapshotContents().writeBytes(data.getBytes(StandardCharsets.UTF_8));
                SnapshotMetadataChange change = new SnapshotMetadataChange.Builder().setDescription(description).build();
                client.commitAndClose(snapshot, change).addOnCompleteListener(x ->
                    send("save", x.isSuccessful(), x.isSuccessful() ? "" : error(x)));
            } catch (Exception e) {
                send("save", false, String.valueOf(e.getMessage()));
            }
        });
    }
}
