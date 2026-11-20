// OG Software shared game services: Apple Game Center side (see Assets/OGPlatform/OGPlatform.cs).
// Results go back to Unity as UnitySendMessage("OGPlatform", "OnNative", "op\nok\ndata").
// Written to compile with or without ARC. Needs iOS 14+ (GameKit's leaderboard APIs).

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GameKit/GameKit.h>

extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

#if __has_feature(objc_arc)
#define OG_AUTORELEASE(x) (x)
#else
#define OG_AUTORELEASE(x) [(x) autorelease]
#endif

// the topmost view controller, to present Apple's screens on
static UIViewController* OGTopController()
{
    UIWindow* window = nil;
    for (UIWindow* w in [UIApplication sharedApplication].windows) if (w.isKeyWindow) { window = w; break; }
    if (!window) window = [UIApplication sharedApplication].windows.firstObject;
    UIViewController* vc = window.rootViewController;
    while (vc.presentedViewController) vc = vc.presentedViewController;
    return vc;
}

static NSString* OGStr(const char* s) { return s ? [NSString stringWithUTF8String:s] : @""; }

static void OGSend(NSString* op, BOOL ok, NSString* data)
{
    NSString* message = [NSString stringWithFormat:@"%@\n%@\n%@", op, ok ? @"1" : @"0", data ?: @""];
    dispatch_async(dispatch_get_main_queue(), ^{ UnitySendMessage("OGPlatform", "OnNative", message.UTF8String); });
}

static NSString* OGError(NSError* e) { return e ? (e.localizedDescription ?: @"error") : @""; }

static NSString* OGClean(NSString* s)
{
    if (!s) return @"";
    s = [s stringByReplacingOccurrencesOfString:@"\t" withString:@" "];
    return [s stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
}

// ------------------------------------------------------------------ Game Center screen

@interface OGGameCenterDelegate : NSObject <GKGameCenterControllerDelegate>
@end

@implementation OGGameCenterDelegate
- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController*)controller
{
    [controller dismissViewControllerAnimated:YES completion:nil];
}
@end

static OGGameCenterDelegate* ogDelegate = nil;

static void OGPresent(GKGameCenterViewController* vc)
{
    if (!ogDelegate) ogDelegate = [[OGGameCenterDelegate alloc] init];
    vc.gameCenterDelegate = ogDelegate;
    [OGTopController() presentViewController:vc animated:YES completion:nil];
}

extern "C" {

// ------------------------------------------------------------------ sign-in

void OGGC_Init()
{
    GKLocalPlayer* player = [GKLocalPlayer localPlayer];
    player.authenticateHandler = ^(UIViewController* viewController, NSError* error) {
        if (viewController)
        {
            [OGTopController() presentViewController:viewController animated:YES completion:nil];
            return;
        }
        GKLocalPlayer* p = [GKLocalPlayer localPlayer];
        if (p.isAuthenticated)
            OGSend(@"auth", YES, [NSString stringWithFormat:@"%@\n%@", p.gamePlayerID ?: @"", OGClean(p.displayName)]);
        else
            OGSend(@"auth", NO, OGError(error));
    };
}

// ------------------------------------------------------------------ achievements

void OGGC_ReportAchievement(const char* key, const char* identifier, double percent, int steps)
{
    NSString* op = [@"ach:" stringByAppendingString:OGStr(key)];
    NSString* sent = [NSString stringWithFormat:@"%d", steps];
    GKAchievement* a = OG_AUTORELEASE([[GKAchievement alloc] initWithIdentifier:OGStr(identifier)]);
    a.percentComplete = percent > 100.0 ? 100.0 : percent;
    a.showsCompletionBanner = YES;
    [GKAchievement reportAchievements:@[a] withCompletionHandler:^(NSError* error) {
        OGSend(op, error == nil, error ? OGError(error) : sent);
    }];
}

void OGGC_ShowAchievements()
{
    OGPresent(OG_AUTORELEASE([[GKGameCenterViewController alloc] initWithState:GKGameCenterViewControllerStateAchievements]));
}

// ------------------------------------------------------------------ leaderboards

void OGGC_SubmitScore(const char* key, const char* identifier, long long score)
{
    NSString* op = [@"lb:" stringByAppendingString:OGStr(key)];
    NSString* sent = [NSString stringWithFormat:@"%lld", score];
    [GKLeaderboard submitScore:(NSInteger)score context:0 player:[GKLocalPlayer localPlayer]
                leaderboardIDs:@[OGStr(identifier)] completionHandler:^(NSError* error) {
        OGSend(op, error == nil, error ? OGError(error) : sent);
    }];
}

void OGGC_LoadTopScores(const char* request, const char* identifier, int count)
{
    NSString* op = OGStr(request);
    if (count < 1) count = 1;
    if (count > 100) count = 100;
    [GKLeaderboard loadLeaderboardsWithIDs:@[OGStr(identifier)] completionHandler:^(NSArray<GKLeaderboard*>* boards, NSError* error) {
        if (error || boards.count == 0) { OGSend(op, NO, OGError(error)); return; }
        [boards[0] loadEntriesForPlayerScope:GKLeaderboardPlayerScopeGlobal timeScope:GKLeaderboardTimeScopeAllTime
                                       range:NSMakeRange(1, count)
                           completionHandler:^(GKLeaderboardEntry* local, NSArray<GKLeaderboardEntry*>* entries, NSInteger total, NSError* error2) {
            if (error2) { OGSend(op, NO, OGError(error2)); return; }
            NSString* me = [GKLocalPlayer localPlayer].gamePlayerID ?: @"";
            NSMutableString* rows = [NSMutableString string];
            for (GKLeaderboardEntry* e in entries)
                [rows appendFormat:@"%ld\t%@\t%ld\t%@\n", (long)e.rank, OGClean(e.player.displayName), (long)e.score,
                    [e.player.gamePlayerID isEqualToString:me] ? @"1" : @"0"];
            OGSend(op, YES, rows);
        }];
    }];
}

void OGGC_ShowLeaderboard(const char* identifier)
{
    NSString* ident = OGStr(identifier);
    GKGameCenterViewController* vc;
    if (ident.length > 0)
        vc = [[GKGameCenterViewController alloc] initWithLeaderboardID:ident playerScope:GKLeaderboardPlayerScopeGlobal
                                                             timeScope:GKLeaderboardTimeScopeAllTime];
    else
        vc = [[GKGameCenterViewController alloc] initWithState:GKGameCenterViewControllerStateLeaderboards];
    OGPresent(OG_AUTORELEASE(vc));
}

// ------------------------------------------------------------------ saved games (need iCloud Drive on the device)

void OGGC_CloudLoad(const char* slot)
{
    NSString* name = OGStr(slot);
    [[GKLocalPlayer localPlayer] fetchSavedGamesWithCompletionHandler:^(NSArray<GKSavedGame*>* games, NSError* error) {
        if (error) { OGSend(@"load", NO, OGError(error)); return; }
        NSMutableArray<GKSavedGame*>* matches = [NSMutableArray array];
        GKSavedGame* newest = nil;
        for (GKSavedGame* g in games)
        {
            if (![g.name isEqualToString:name]) continue;
            [matches addObject:g];
            if (!newest || [g.modificationDate compare:newest.modificationDate] == NSOrderedDescending) newest = g;
        }
        if (!newest) { OGSend(@"load", YES, @""); return; }
        [newest loadDataWithCompletionHandler:^(NSData* data, NSError* error2) {
            if (error2) { OGSend(@"load", NO, OGError(error2)); return; }
            NSString* text = data ? OG_AUTORELEASE([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding]) : @"";
            // two devices saved at once: keep the newest copy and clear the conflict
            if (matches.count > 1 && data)
                [[GKLocalPlayer localPlayer] resolveConflictingSavedGames:matches withData:data completionHandler:nil];
            OGSend(@"load", YES, text ?: @"");
        }];
    }];
}

void OGGC_CloudSave(const char* slot, const char* text)
{
    NSData* data = [OGStr(text) dataUsingEncoding:NSUTF8StringEncoding];
    [[GKLocalPlayer localPlayer] saveGameData:data withName:OGStr(slot) completionHandler:^(GKSavedGame* saved, NSError* error) {
        OGSend(@"save", error == nil, OGError(error));
    }];
}

}
