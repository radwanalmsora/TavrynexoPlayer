import QtQuick
import QtQuick.Controls
import Tavrynexo.Vulkan 1.0
import ".."

Rectangle {
    id: root
    color: Theme.midnightBlack

    property var player: null
    property bool isFullscreen: false
    signal toggleFullscreen()
    signal togglePlaylist()

    // محرك عرض الإطارات بـ Vulkan
    VulkanRenderer {
        id: vulkanCanvas
        anchors.fill: parent
        visible: player && player.duration > 0
    }

    Connections {
        target: player
        function onFrameReady(frame) {
            vulkanCanvas.updateFrame(frame);
        }
    }

    // الواجهة الترحيبية السينمائية عند عدم فتح ملف
    Column {
        anchors.centerIn: parent
        spacing: 16
        visible: !player || player.duration === 0

        Rectangle {
            width: 72
            height: 72
            radius: 36
            color: Theme.deepCharcoal
            border.color: Theme.darkBorder
            border.width: 1.5
            anchors.horizontalCenter: parent.horizontalCenter

            Text {
                anchors.centerIn: parent
                text: "▶"
                font.pixelSize: 26
                color: Theme.cyan
            }
        }

        Text {
            text: "Tavrynexo Cinematic Player"
            color: Theme.pureWhite
            font.family: Theme.fontFamily
            font.bold: true
            font.pixelSize: 18
            anchors.horizontalCenter: parent.horizontalCenter
        }

        Text {
            text: "Drop 4K / HDR video files here or click Open File"
            color: Theme.softGray
            font.family: Theme.fontFamily
            font.pixelSize: 12
            anchors.horizontalCenter: parent.horizontalCenter
        }
    }

    // تتبع حركة الماوس والنقر المزدوج
    MouseArea {
        id: viewportMouseArea
        anchors.fill: parent
        hoverEnabled: true
        cursorShape: controlsOverlay.showControls ? Qt.ArrowCursor : Qt.BlankCursor

        onPositionChanged: controlsOverlay.resetHideTimer()
        onDoubleClicked: root.toggleFullscreen()
        onClicked: {
            if (controlsOverlay.showControls) {
                if (player) player.togglePlay();
            } else {
                controlsOverlay.resetHideTimer();
            }
        }
    }

    // أدوات التحكم العائمة التلقائية الإخفاء
    ControlsOverlay {
        id: controlsOverlay
        anchors.fill: parent
        player: root.player
        isFullscreen: root.isFullscreen
        onToggleFullscreen: root.toggleFullscreen()
        onTogglePlaylist: root.togglePlaylist()
    }
}
