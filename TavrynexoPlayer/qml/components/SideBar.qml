import QtQuick
import QtQuick.Controls
import ".."

Rectangle {
    id: root
    width: 220
    color: Theme.darkSlate
    border.color: Theme.darkBorder
    border.width: 1

    property string currentSection: "Home"
    signal sectionSelected(string section)

    Column {
        anchors.fill: parent
        anchors.margins: 14
        spacing: 16

        // قسم LIBRARY
        Text {
            text: "LIBRARY"
            color: Theme.softGray
            font.family: Theme.fontFamily
            font.pixelSize: 11
            font.bold: true
            font.letterSpacing: 1.5
        }

        Column {
            width: parent.width
            spacing: 3

            Repeater {
                model: [
                    { name: "Home", icon: "◈" },
                    { name: "Videos", icon: "▣" },
                    { name: "Music", icon: "♫" },
                    { name: "Recent", icon: "◷" },
                    { name: "Favorites", icon: "♥" }
                ]

                delegate: Rectangle {
                    width: parent.width
                    height: 36
                    radius: 6
                    color: (root.currentSection === modelData.name) ? Theme.slateGray : 
                           (itemMouseArea.containsMouse ? Qt.rgba(1,1,1,0.04) : "transparent")
                    border.color: (root.currentSection === modelData.name) ? Theme.electricBlue : "transparent"
                    border.width: 1

                    Row {
                        anchors.left: parent.left
                        anchors.leftMargin: 12
                        anchors.verticalCenter: parent.verticalCenter
                        spacing: 12

                        Text {
                            text: modelData.icon
                            color: (root.currentSection === modelData.name) ? Theme.cyan : Theme.softGray
                            font.pixelSize: 13
                        }
                        Text {
                            text: modelData.name
                            color: (root.currentSection === modelData.name) ? Theme.pureWhite : Theme.softGray
                            font.family: Theme.fontFamily
                            font.pixelSize: 13
                            font.bold: (root.currentSection === modelData.name)
                        }
                    }

                    MouseArea {
                        id: itemMouseArea
                        anchors.fill: parent
                        hoverEnabled: true
                        cursorShape: Qt.PointingHandCursor
                        onClicked: {
                            root.currentSection = modelData.name;
                            root.sectionSelected(modelData.name);
                        }
                    }
                }
            }
        }

        Rectangle { width: parent.width; height: 1; color: Theme.darkBorder }

        // قسم PLAYLISTS
        Row {
            width: parent.width
            Text {
                text: "PLAYLISTS"
                color: Theme.softGray
                font.family: Theme.fontFamily
                font.pixelSize: 11
                font.bold: true
                font.letterSpacing: 1.5
            }
        }

        Rectangle {
            width: parent.width
            height: 34
            radius: 6
            color: createPlMouse.containsMouse ? Theme.slateGray : "transparent"
            border.color: Theme.darkBorder

            Row {
                anchors.left: parent.left
                anchors.leftMargin: 12
                anchors.verticalCenter: parent.verticalCenter
                spacing: 10

                Text { text: "+"; color: Theme.cyan; font.bold: true; font.pixelSize: 15 }
                Text { text: "Create Playlist"; color: Theme.pureWhite; font.family: Theme.fontFamily; font.pixelSize: 12 }
            }

            MouseArea {
                id: createPlMouse
                anchors.fill: parent
                hoverEnabled: true
                cursorShape: Qt.PointingHandCursor
                onClicked: root.sectionSelected("CreatePlaylist")
            }
        }

        Item { width: 1; height: 30 }

        // قسم SETTINGS
        Text {
            text: "SETTINGS"
            color: Theme.softGray
            font.family: Theme.fontFamily
            font.pixelSize: 11
            font.bold: true
            font.letterSpacing: 1.5
        }

        Rectangle {
            width: parent.width
            height: 36
            radius: 6
            color: (root.currentSection === "Preferences") ? Theme.slateGray : 
                   (prefMouse.containsMouse ? Qt.rgba(1,1,1,0.04) : "transparent")

            Row {
                anchors.left: parent.left
                anchors.leftMargin: 12
                anchors.verticalCenter: parent.verticalCenter
                spacing: 12

                Text { text: "⚙"; color: Theme.softGray; font.pixelSize: 13 }
                Text { text: "Preferences"; color: Theme.pureWhite; font.family: Theme.fontFamily; font.pixelSize: 13 }
            }

            MouseArea {
                id: prefMouse
                anchors.fill: parent
                hoverEnabled: true
                cursorShape: Qt.PointingHandCursor
                onClicked: {
                    root.currentSection = "Preferences";
                    root.sectionSelected("Preferences");
                }
            }
        }
    }
}
