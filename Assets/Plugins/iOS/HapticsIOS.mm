// Rung haptic iOS cho Haptics.cs (Unity tự chỉ build file này cho iOS vì nằm trong Plugins/iOS).
// type: 0 Light, 1 Medium, 2 Heavy, 3 Success, 4 Failure (khớp enum HapticType).
#import <UIKit/UIKit.h>

extern "C" void _HapticsPlay(int type)
{
    if (type >= 3)
    {
        UINotificationFeedbackGenerator *gen = [[UINotificationFeedbackGenerator alloc] init];
        [gen notificationOccurred:(type == 3 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError)];
        return;
    }
    UIImpactFeedbackStyle style = type == 0 ? UIImpactFeedbackStyleLight
                                : type == 1 ? UIImpactFeedbackStyleMedium
                                            : UIImpactFeedbackStyleHeavy;
    UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    [gen impactOccurred];
}
