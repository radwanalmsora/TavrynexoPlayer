import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import Tavrynexo.Core 1.0
import "components"

ApplicationWindow {
    id: window
    width: 1280
    height: 768
    minimumWidth: 960
    minimumHeight: 540
    visible: true
    title: "Tavrynexo Player"
    color: Theme.midnightBlack

    // نافذة ديسكتوب سينمائية بدون إطار خارجي افتراضي
    flags: Qt.Window | Qt.FramelessWindowHint

    property bool isFullscreen: false
    property bool isPlaylistOpen: true

    // متحكم المشغل المركزي
    PlayerController {
        id: playerController
    }

    Component.onCompleted: {
        if (initialFileArgument && initialFileArgument.length > 0) {
            playerController.openFile(initialFileArgument);
        }
    }

    // تحريك النافذة عبر السحب من الشريط العلوي
    MouseArea {
        id: dragArea
        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        height: 48
        property point clickPos: "0,0"
        onPressed: (mouse) => { clickPos = Qt.point(mouse.x, mouse.y) }
        onPositionChanged: (mouse) => {
            if (pressed && !window.isFullscreen) {
                var delta = Qt.point(mouse.x - clickPos.x, mouse.y - clickPos.y);
                window.x += delta.x;
                window.y += delta.y;
            }
        }
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        // المنطقة الأولى: الشريط العلوي
        TopBar {
            id: topBar
            Layout.fillWidth: true
            Layout.preferredHeight: 48
            player: playerController
            windowRef: window
            visible: !window.isFullscreen
        }

        // المساحة الوسطى (القائمة اليسرى + مساحة الفيديو + قائمة التشغيل اليمنى)
        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            spacing: 0

            // المنطقة الثانية: القائمة الجانبية اليسرى
            SideBar {
                id: sideBar
                Layout.preferredWidth: 220
                Layout.fillHeight: true
                visible: !window.isFullscreen
            }

            // المنطقة الثالثة: مساحة الفيديو المركزية
            VideoViewport {
                id: videoViewport
                Layout.fillWidth: true
                Layout.fillHeight: true
                player: playerController
                isFullscreen: window.isFullscreen
                onToggleFullscreen: {
                    window.isFullscreen = !window.isFullscreen;
                    if (window.isFullscreen) {
                        window.showFullScreen();
                    } else {
                        window.showNormal();
                    }
                }
                onTogglePlaylist: {
                    window.isPlaylistOpen = !window.isPlaylistOpen;
                }
            }

            // المنطقة الرابعة: قائمة التشغيل اليمنى (قابلة للإخفاء والتوسيع)
            PlaylistDrawer {
                id: playlistDrawer
                Layout.preferredWidth: 280
                Layout.fillHeight: true
                player: playerController
                visible: window.isPlaylistOpen && !window.isFullscreen
                onCloseRequested: window.isPlaylistOpen = false
            }
        }
    }

    // اختصارات لوحة المفاتيح المتقدمة للديسكتوب
    Shortcut { sequence: "Space"; onActivated: playerController.togglePlay() }
    Shortcut { sequence: "Left"; onActivated: playerController.backward10() }
    Shortcut { sequence: "Right"; onActivated: playerController.forward10() }
    Shortcut { sequence: "Up"; onActivated: playerController.setVolume(playerController.volume + 0.05) }
    Shortcut { sequence: "Down"; onActivated: playerController.setVolume(playerController.volume - 0.05) }
    Shortcut { sequence: "M"; onActivated: playerController.setMuted(!playerController.isMuted) }
    Shortcut { 
        sequence: "F"
        onActivated: {
            window.isFullscreen = !window.isFullscreen;
            if (window.isFullscreen) window.showFullScreen(); else window.showNormal();
        }
    }
    Shortcut { 
        sequence: "Esc"
        onActivated: {
            if (window.isFullscreen) {
                window.isFullscreen = false;
                window.showNormal();
            }
        }
    }
}
