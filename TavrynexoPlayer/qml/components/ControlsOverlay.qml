import QtQuick
import QtQuick.Controls
import ".."

Item {
    id: root
    anchors.fill: parent

    property var player: null
    property bool isFullscreen: false
    signal toggleFullscreen()
    signal togglePlaylist()

    // مؤقت الإخفاء التلقائي بعد 2.5 ثانية من سكون الماوس
    property bool showControls: true
    Timer {
        id: hideTimer
        interval: 2500
        running: player && player.isPlaying && root.showControls
        repeat: false
        onTriggered: root.showControls = false
    }

    function resetHideTimer() {
        root.showControls = true;
        hideTimer.restart();
    }

    // دالة تنسيق الوقت (HH:MM:SS)
    function formatTime(totalSeconds) {
        if (isNaN(totalSeconds) || totalSeconds < 0) totalSeconds = 0;
        var hrs = Math.floor(totalSeconds / 3600);
        var mins = Math.floor((totalSeconds % 3600) / 60);
        var secs = Math.floor(totalSeconds % 60);
        if (hrs > 0) {
            return (hrs < 10 ? "0" : "") + hrs + ":" + 
                   (mins < 10 ? "0" : "") + mins + ":" + 
                   (secs < 10 ? "0" : "") + secs;
        }
        return (mins < 10 ? "0" : "") + mins + ":" + 
               (secs < 10 ? "0" : "") + secs;
    }

    // الطبقة الشفافة العائمة (HUD Overlay)
    Rectangle {
        id: controlsBar
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.margins: 16
        height: 88
        radius: 12
        color: Qt.rgba(17/255, 21/255, 29/255, 0.92) // Deep Charcoal with blur feel
        border.color: Theme.darkBorder
        border.width: 1

        opacity: root.showControls ? 1.0 : 0.0
        visible: opacity > 0.0
        Behavior on opacity { NumberAnimation { duration: 250 } }

        Column {
            anchors.fill: parent
            anchors.margins: 10
            spacing: 6

            // 1. شريط التقدم (Timeline Slider) باللون السماوي المميز Cyan
            Item {
                width: parent.width
                height: 18

                // مسار الخلفية
                Rectangle {
                    id: progressTrack
                    anchors.verticalCenter: parent.verticalCenter
                    width: parent.width
                    height: 5
                    radius: 2.5
                    color: Theme.slateGray

                    // مسار التقدم بلون الـ Cyan
                    Rectangle {
                        height: parent.height
                        width: player && player.duration > 0 ? 
                               (parent.width * (player.position / player.duration)) : 0
                        radius: 2.5
                        color: Theme.cyan
                    }
                }

                // مقبض التقدم المتوهج
                Rectangle {
                    width: 14
                    height: 14
                    radius: 7
                    color: Theme.pureWhite
                    border.color: Theme.cyan
                    border.width: 2
                    anchors.verticalCenter: parent.verticalCenter
                    x: Math.max(0, Math.min(progressTrack.width - width, 
                       (player && player.duration > 0 ? (progressTrack.width * (player.position / player.duration)) - (width / 2) : 0)))
                    visible: progressMouseArea.containsMouse || progressMouseArea.pressed
                }

                MouseArea {
                    id: progressMouseArea
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onPositionChanged: (mouse) => {
                        root.resetHideTimer();
                        if (pressed && player && player.duration > 0) {
                            var posRatio = Math.max(0, Math.min(1, mouse.x / width));
                            player.seek(posRatio * player.duration);
                        }
                    }
                    onClicked: (mouse) => {
                        if (player && player.duration > 0) {
                            var posRatio = Math.max(0, Math.min(1, mouse.x / width));
                            player.seek(posRatio * player.duration);
                        }
                    }
                }
            }

            // 2. أزرار التحكم السفلية
            Row {
                width: parent.width
                anchors.leftMargin: 6
                anchors.rightMargin: 6
                spacing: 14

                // زر تأخير 10 ثوانٍ
                Button {
                    width: 32
                    height: 32
                    background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent"; radius: 6 }
                    contentItem: Text { text: "↺10"; color: Theme.pureWhite; font.pixelSize: 11; horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter }
                    onClicked: { root.resetHideTimer(); if (player) player.backward10(); }
                }

                // زر التشغيل الدائري الفاخر (القلب النابض)
                CircularPlayButton {
                    width: 42
                    height: 42
                    isPlaying: player ? player.isPlaying : false
                    onClicked: {
                        root.resetHideTimer();
                        if (player) player.togglePlay();
                    }
                }

                // زر تقديم 10 ثوانٍ
                Button {
                    width: 32
                    height: 32
                    background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent"; radius: 6 }
                    contentItem: Text { text: "10↻"; color: Theme.pureWhite; font.pixelSize: 11; horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter }
                    onClicked: { root.resetHideTimer(); if (player) player.forward10(); }
                }

                // توقيت العرض (Current / Duration)
                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    text: (player ? root.formatTime(player.position) : "00:00") + " / " + 
                          (player ? root.formatTime(player.duration) : "00:00")
                    color: Theme.softGray
                    font.family: Theme.fontFamily
                    font.pixelSize: 12
                }

                // شارة 4K
                Rectangle {
                    anchors.verticalCenter: parent.verticalCenter
                    width: 32
                    height: 18
                    radius: 3
                    color: Theme.slateGray
                    border.color: Theme.electricBlue
                    border.width: 1
                    visible: player && player.videoWidth >= 3840
                    Text {
                        anchors.centerIn: parent
                        text: "4K"
                        color: Theme.cyan
                        font.pixelSize: 10
                        font.bold: true
                    }
                }

                // شارة HDR10
                Rectangle {
                    anchors.verticalCenter: parent.verticalCenter
                    width: 36
                    height: 18
                    radius: 3
                    color: Theme.slateGray
                    border.color: Theme.emeraldGreen
                    border.width: 1
                    visible: player && player.isHDR
                    Text {
                        anchors.centerIn: parent
                        text: "HDR"
                        color: Theme.emeraldGreen
                        font.pixelSize: 10
                        font.bold: true
                    }
                }

                Item { width: 10; height: 1 } // الفاصل

                // مساحة مرنة
                Item {
                    id: spacer
                    width: parent.width - 480
                    height: 1
                }

                // التحكم بالصوت (Volume Icon + Slider)
                Row {
                    anchors.verticalCenter: parent.verticalCenter
                    spacing: 6

                    Text {
                        text: (player && player.isMuted) ? "🔇" : ((player && player.volume > 0.5) ? "🔊" : "🔉")
                        color: Theme.softGray
                        font.pixelSize: 14
                        anchors.verticalCenter: parent.verticalCenter
                        MouseArea {
                            anchors.fill: parent
                            cursorShape: Qt.PointingHandCursor
                            onClicked: {
                                root.resetHideTimer();
                                if (player) player.setMuted(!player.isMuted);
                            }
                        }
                    }

                    Rectangle {
                        width: 80
                        height: 4
                        radius: 2
                        color: Theme.slateGray
                        anchors.verticalCenter: parent.verticalCenter

                        Rectangle {
                            height: parent.height
                            width: player ? (parent.width * (player.isMuted ? 0 : player.volume)) : 0
                            radius: 2
                            color: Theme.cyan
                        }

                        MouseArea {
                            anchors.fill: parent
                            cursorShape: Qt.PointingHandCursor
                            onPositionChanged: (mouse) => {
                                root.resetHideTimer();
                                if (pressed && player) {
                                    var vol = Math.max(0, Math.min(1, mouse.x / width));
                                    player.setVolume(vol);
                                }
                            }
                            onClicked: (mouse) => {
                                if (player) {
                                    var vol = Math.max(0, Math.min(1, mouse.x / width));
                                    player.setVolume(vol);
                                }
                            }
                        }
                    }
                }

                // زر الترجمة والمسار الصوتي
                Button {
                    width: 32
                    height: 32
                    background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent"; radius: 6 }
                    contentItem: Text { text: "CC"; color: Theme.pureWhite; font.pixelSize: 11; font.bold: true; horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter }
                    onClicked: root.resetHideTimer()
                }

                // زر إظهار/إخفاء قائمة التشغيل اليمنى
                Button {
                    width: 32
                    height: 32
                    background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent"; radius: 6 }
                    contentItem: Text { text: "☰"; color: Theme.pureWhite; font.pixelSize: 14; horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter }
                    onClicked: { root.resetHideTimer(); root.togglePlaylist(); }
                }

                // زر ملء الشاشة (Fullscreen)
                Button {
                    width: 32
                    height: 32
                    background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent"; radius: 6 }
                    contentItem: Text { text: root.isFullscreen ? "⤦" : "⛶"; color: Theme.pureWhite; font.pixelSize: 13; horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter }
                    onClicked: { root.resetHideTimer(); root.toggleFullscreen(); }
                }
            }
        }
    }
}
