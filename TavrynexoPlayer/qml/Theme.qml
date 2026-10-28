pragma Singleton
import QtQuick

QtObject {
    // الألوان المحددة في البرومبت بدقة تامة
    readonly property color midnightBlack: "#0B0D12" // الخلفية الرئيسية
    readonly property color deepCharcoal:  "#11151D" // خلفية المشغل
    readonly property color darkSlate:     "#151A24" // القائمة الجانبية
    readonly property color slateGray:     "#1C2330" // البطاقات
    readonly property color electricBlue:  "#4F8CFF" // اللون الأساسي للتفاعل
    readonly property color cyan:          "#36D6E7" // اللون الثانوي للتقدم والصوت
    readonly property color pureWhite:     "#F5F7FA" // النص الرئيسي
    readonly property color softGray:      "#98A2B3" // النص الثانوي
    readonly property color darkBorder:    "#293142" // حدود العناصر
    readonly property color emeraldGreen:  "#35D49A" // حالة التشغيل النشط

    // الخطوط
    readonly property string fontFamily: "Segoe UI, -apple-system, sans-serif"
}
