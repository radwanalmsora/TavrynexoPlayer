import QtQuick
import QtQuick.Controls
import ".."

Item {
    id: root
    width: 64
    height: 64

    property bool isPlaying: false
    signal clicked()

    Rectangle {
        id: bgCircle
        anchors.fill: parent
        radius: width / 2
        color: mouseArea.containsPress ? Qt.darker(Theme.electricBlue, 1.2) : 
               mouseArea.containsMouse ? Theme.electricBlue : Theme.slateGray
        border.color: mouseArea.containsMouse ? Theme.cyan : Theme.darkBorder
        border.width: 1.5

        scale: mouseArea.containsPress ? 0.92 : (mouseArea.containsMouse ? 1.05 : 1.0)
        Behavior on scale { NumberAnimation { duration: 150; easing.type: Easing.OutQuad } }
        Behavior on color { ColorAnimation { duration: 150 } }

        // أيقونة التشغيل / الإيقاف المؤقت
        Canvas {
            id: iconCanvas
            anchors.centerIn: parent
            width: 24
            height: 24
            onPaint: {
                var ctx = getContext("2d");
                ctx.clearRect(0, 0, width, height);
                ctx.fillStyle = Theme.pureWhite;

                if (!root.isPlaying) {
                    // مثلث التشغيل (Play Triangle)
                    ctx.beginPath();
                    ctx.moveTo(7, 4);
                    ctx.lineTo(20, 12);
                    ctx.lineTo(7, 20);
                    ctx.closePath();
                    ctx.fill();
                } else {
                    // عمودا الإيقاف (Pause Bars)
                    ctx.fillRect(6, 4, 4, 16);
                    ctx.fillRect(14, 4, 4, 16);
                }
            }
        }

        Connections {
            target: root
            function onIsPlayingChanged() { iconCanvas.requestPaint(); }
        }
    }

    MouseArea {
        id: mouseArea
        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.clicked()
    }
}
