import QtQuick
import QtQuick.Controls
import QtQuick.Dialogs
import ".."

Rectangle {
    id: root
    height: 48
    color: Theme.midnightBlack
    border.color: Theme.darkBorder
    border.width: 1

    property var player: null
    property var windowRef: null

    signal toggleSettings()

    FileDialog {
        id: fileDialog
        title: "Open Video File - Tavrynexo Player"
        nameFilters: ["Video Files (*.mp4 *.mkv *.avi *.webm *.mov *.flv)", "All Files (*)"]
        onAccepted: {
            if (player) player.openFile(selectedFile);
        }
    }

    FolderDialog {
        id: folderDialog
        title: "Open Folder - Tavrynexo Player"
        onAccepted: {
            if (player) player.openFolder(selectedFolder);
        }
    }

    Row {
        anchors.left: parent.left
        anchors.leftMargin: 16
        anchors.verticalCenter: parent.verticalCenter
        spacing: 14

        // الشعار السينمائي الفاخر
        Rectangle {
            width: 28
            height: 28
            radius: 6
            color: Theme.slateGray
            border.color: Theme.electricBlue
            border.width: 1.5
            anchors.verticalCenter: parent.verticalCenter

            Text {
                anchors.centerIn: parent
                text: "T"
                font.family: Theme.fontFamily
                font.bold: true
                font.pixelSize: 15
                color: Theme.cyan
            }
        }

        // اسم البرنامج
        Text {
            text: "Tavrynexo Player"
            font.family: Theme.fontFamily
            font.pixelSize: 14
            font.bold: true
            color: Theme.pureWhite
            anchors.verticalCenter: parent.verticalCenter
        }

        Item { width: 12; height: 1 }

        // زر فتح ملف
        Button {
            id: openFileBtn
            height: 30
            text: "Open File"
            contentItem: Text {
                text: parent.text
                color: openFileBtn.hovered ? Theme.cyan : Theme.pureWhite
                font.family: Theme.fontFamily
                font.pixelSize: 12
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
            }
            background: Rectangle {
                color: openFileBtn.hovered ? Theme.slateGray : "transparent"
                radius: 4
                border.color: openFileBtn.hovered ? Theme.darkBorder : "transparent"
            }
            onClicked: fileDialog.open()
        }

        // زر فتح مجلد
        Button {
            id: openFolderBtn
            height: 30
            text: "Open Folder"
            contentItem: Text {
                text: parent.text
                color: openFolderBtn.hovered ? Theme.cyan : Theme.pureWhite
                font.family: Theme.fontFamily
                font.pixelSize: 12
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
            }
            background: Rectangle {
                color: openFolderBtn.hovered ? Theme.slateGray : "transparent"
                radius: 4
                border.color: openFolderBtn.hovered ? Theme.darkBorder : "transparent"
            }
            onClicked: folderDialog.open()
        }
    }

    // شريط البحث في مكتبة الوسائط
    Rectangle {
        width: 220
        height: 28
        radius: 14
        color: Theme.deepCharcoal
        border.color: searchInput.activeFocus ? Theme.electricBlue : Theme.darkBorder
        border.width: 1
        anchors.centerIn: parent

        TextInput {
            id: searchInput
            anchors.fill: parent
            anchors.leftMargin: 12
            anchors.rightMargin: 12
            verticalAlignment: TextInput.AlignVCenter
            color: Theme.pureWhite
            font.family: Theme.fontFamily
            font.pixelSize: 11
            clip: true

            Text {
                text: "Search media library..."
                color: Theme.softGray
                font.family: Theme.fontFamily
                font.pixelSize: 11
                anchors.fill: parent
                verticalAlignment: Text.AlignVCenter
                visible: !searchInput.text && !searchInput.activeFocus
            }
        }
    }

    // أزرار جهة اليمين (الإعدادات وأزرار النافذة)
    Row {
        anchors.right: parent.right
        anchors.verticalCenter: parent.verticalCenter
        spacing: 0

        // زر الإعدادات
        Button {
            width: 44
            height: 48
            background: Rectangle {
                color: parent.hovered ? Theme.slateGray : "transparent"
            }
            contentItem: Text {
                text: "⚙"
                color: Theme.softGray
                font.pixelSize: 14
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
            }
            onClicked: root.toggleSettings()
        }

        // زر تصغير النافذة
        Button {
            width: 44
            height: 48
            background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent" }
            contentItem: Text {
                text: "—"
                color: Theme.pureWhite
                font.pixelSize: 11
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
            }
            onClicked: if (windowRef) windowRef.showMinimized()
        }

        // زر تكبير / استعادة النافذة
        Button {
            width: 44
            height: 48
            background: Rectangle { color: parent.hovered ? Theme.slateGray : "transparent" }
            contentItem: Text {
                text: (windowRef && windowRef.visibility === Window.Maximized) ? "❐" : "▢"
                color: Theme.pureWhite
                font.pixelSize: 12
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
            }
            onClicked: {
                if (!windowRef) return;
                if (windowRef.visibility === Window.Maximized) {
                    windowRef.showNormal();
                } else {
                    windowRef.showMaximized();
                }
            }
        }

        // زر إغلاق النافذة
        Button {
            id: closeBtn
            width: 46
            height: 48
            background: Rectangle {
                color: closeBtn.hovered ? "#E81123" : "transparent"
            }
            contentItem: Text {
                text: "✕"
                color: Theme.pureWhite
                font.pixelSize: 11
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
            }
            onClicked: if (windowRef) windowRef.close()
        }
    }
}
