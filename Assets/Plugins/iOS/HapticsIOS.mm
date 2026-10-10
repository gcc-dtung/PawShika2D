// Rung haptic iOS cho Haptics.cs (Unity tự chỉ build file này cho iOS vì nằm trong Plugins/iOS).
// type khớp enum HapticType: 0 Light, 1 Medium, 2 Heavy, 3 Success, 4 Failure, 5 Selection, 6 Soft, 7 Rigid,
// 8 Warning, 9 Tick, 10 Rise, 11 Thud, 12 Coin, 13 Button. Mức mạnh (intensity 0–1) cần iOS 13+, máy cũ dùng mức mặc định.
#import <UIKit/UIKit.h>

static void Impact(UIImpactFeedbackStyle style, CGFloat intensity)
{
    UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    if (@available(iOS 13.0, *)) [gen impactOccurredWithIntensity:intensity];
    else [gen impactOccurred];
}

static void Notify(UINotificationFeedbackType type)
{
    UINotificationFeedbackGenerator *gen = [[UINotificationFeedbackGenerator alloc] init];
    [gen notificationOccurred:type];
}

static UIImpactFeedbackStyle SoftStyle()
{
    if (@available(iOS 13.0, *)) return UIImpactFeedbackStyleSoft;
    return UIImpactFeedbackStyleLight;
}

static UIImpactFeedbackStyle RigidStyle()
{
    if (@available(iOS 13.0, *)) return UIImpactFeedbackStyleRigid;
    return UIImpactFeedbackStyleMedium;
}

extern "C" void _HapticsPlay(int type)
{
    switch (type)
    {
        case 0: Impact(UIImpactFeedbackStyleLight, 0.6); break;
        case 1: Impact(UIImpactFeedbackStyleMedium, 0.7); break;
        case 2: Impact(UIImpactFeedbackStyleHeavy, 0.8); break;
        case 3: Notify(UINotificationFeedbackTypeSuccess); break;
        case 4: Notify(UINotificationFeedbackTypeError); break;
        case 5: [[[UISelectionFeedbackGenerator alloc] init] selectionChanged]; break;
        case 6: Impact(SoftStyle(), 0.7); break;
        case 7: Impact(RigidStyle(), 0.6); break;
        case 8: Notify(UINotificationFeedbackTypeWarning); break;
        case 9: Impact(RigidStyle(), 0.4); break;
        case 10: Impact(SoftStyle(), 1.0); break;
        case 11: Impact(UIImpactFeedbackStyleHeavy, 0.7); break;
        case 12: Impact(UIImpactFeedbackStyleLight, 0.45); break;
        case 13: Impact(UIImpactFeedbackStyleLight, 0.5); break;
    }
}
