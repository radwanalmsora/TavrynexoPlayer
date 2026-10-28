import QtQuick
import QtQuick.Controls
import ".."

Rectangle {
    id: root
    width: 280
    color: Theme.deepCharcoal
    border.color: Theme.darkBorder
    border.width: 1

    property var player: null
    property bool isOpen: true
    signal closeRequested()

    Behavior on width { NumberAnimation { duration: 200; easing.type: Easing.InOutQuad } }

    Column {
        anchors.fill: parent
        anchors.margins: 14
        spacing: 12

        // الشريط العلوي للقائمة
        Row {
            width: parent.width
            Text {
                text: "PLAYLIST"
                color: Theme.pureWhite
                font.family: Theme.fontFamily
                font.bold: true
                font.pixelSize: 12
                font.letterSpacing: 1.5
                anchors.verticalCenter: parent.verticalCenter
            }

            Item {
                width: parent.width - 150
                height: 1
            }

            Text {
                text: (player && player.playlist ? player.playlist.length : 0) + " items"
                color: Theme.softGray
                font.family: Theme.fontFamily
                font.pixelSize: 11
                anchors.verticalCenter: parent.verticalCenter
            }

            // زر إخفاء القائمة لتوسيع الفيديو
            Button {
                width: 26
                height: 26
                background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent"; radius: 4 }
                contentItem: Text { text: "✕"; color: Theme.softGray; font.pixelSize: 11; horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter }
                onClicked: root.closeRequested()
            }
        }

        Rectangle { width: parent.width; height: 1; color: Theme.darkBorder }

        // قائمة العناصر (Items List)
        ListView {
            id: playlistView
            width: parent.width
            height: parent.height - 80
            clip: true
            spacing: 6
            model: player ? player.playlist : []

            delegate: Rectangle {
                width: playlistView.width
                height: 48
                radius: 6
                color: (player && player.currentTrackIndex === index) ? Theme.slateGray : 
                       (itemMouse.containsMouse ? Qt.rgba(1,1,1,0.03) : "transparent")
                border.color: (player && player.currentTrackIndex === index) ? Theme.electricBlue : "transparent"
                border.width: 1

                Row {
                    anchors.fill: parent
                    anchors.margins: 8
                    spacing: 10

                    // مؤشر التشغيل الزمردي النشط
                    Rectangle {
                        width: 4
                        height: 24
                        radius: 2
                        color: (player && player.currentTrackIndex === index) ? Theme.emeraldGreen : "transparent"
                        anchors.verticalCenter: parent.verticalCenter
                    }

                    Column {
                        width: parent.width - 40
                        anchors.verticalCenter: parent.verticalCenter
                        spacing: 3

                        Text {
                            text: {
                                var parts = modelData.split("/");
                                return parts[parts.length - 1];
                            }
                            color: (player && player.currentTrackIndex === index) ? Theme.pureWhite : Theme.softGray
                            font.family: Theme.fontFamily
                            font.pixelSize: 12
                            font.bold: (player && player.currentTrackIndex === index)
                            elide: Text.ElideRight
                            width: parent.width
                        }

                        Text {
                            text: (player && player.currentTrackIndex === index && player.isPlaying) ? "Playing now" : "Queued"
                            color: (player && player.currentTrackIndex === index) ? Theme.cyan : Theme.softGray
                            font.family: Theme.fontFamily
                            font.pixelSize: 10
                        }
                    }
                }

                MouseArea {
                    id: itemMouse
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onClicked: {
                        if (player) player.playPlaylistItem(index);
                    }
                }
            }
        }
    }
}
