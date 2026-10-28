#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QQmlContext>
#include <QQuickWindow>
#include <QIcon>
#include <iostream>

#include "core/PlayerController.hpp"
#include "vulkan/VulkanRenderer.hpp"

int main(int argc, char *argv[]) {
    // تفعيل الـ High DPI للـ 4K Monitors
    QGuiApplication::setHighDpiScaleFactorRoundingPolicy(
        Qt::HighDpiScaleFactorRoundingPolicy::PassThrough
    );

    QGuiApplication app(argc, argv);
    app.setOrganizationName("Tavrynexo");
    app.setOrganizationDomain("tavrynexo.internal");
    app.setApplicationName("Tavrynexo Player");
    app.setApplicationVersion("1.0.0");

    // تفعيل محرك Vulkan لـ Qt Quick Scene Graph
    QQuickWindow::setGraphicsApi(QSGRendererInterface::Vulkan);

    // تسجيل أنواع C++ لتكون متاحة مباشرة داخل عناصر QML
    qmlRegisterType<Tavrynexo::Core::PlayerController>("Tavrynexo.Core", 1, 0, "PlayerController");
    qmlRegisterType<Tavrynexo::Vulkan::VulkanRenderer>("Tavrynexo.Vulkan", 1, 0, "VulkanRenderer");

    QQmlApplicationEngine engine;

    // معالجة فتح الملف إذا تم تمريره كمعامل سطر أوامر (e.g. TavrynexoPlayer.exe movie.mkv)
    QString initialFile;
    if (argc > 1) {
        initialFile = QString::fromLocal8Bit(argv[1]);
    }
    engine.rootContext()->setContextProperty("initialFileArgument", initialFile);

    const QUrl url(QStringLiteral("qrc:/qml/main.qml"));
    QObject::connect(
        &engine,
        &QQmlApplicationEngine::objectCreated,
        &app,
        [url](QObject *obj, const QUrl &objUrl) {
            if (!obj && url == objUrl) {
                std::cerr << "[Tavrynexo::Fatal] Failed to load main.qml" << std::endl;
                QCoreApplication::exit(-1);
            }
        },
        Qt::QueuedConnection
    );

    engine.load(url);

    return app.exec();
}
