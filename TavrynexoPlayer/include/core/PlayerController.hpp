#pragma once

#include <QObject>
#include <QString>
#include <QStringList>
#include <QVariantList>
#include <QImage>
#include <memory>
#include <thread>
#include <atomic>

#include "core/Demuxer.hpp"
#include "core/VideoDecoder.hpp"
#include "core/AudioDecoder.hpp"
#include "core/AudioEngine.hpp"
#include "core/ClockSync.hpp"

namespace Tavrynexo::Core {

class PlayerController : public QObject {
    Q_OBJECT

    // الخصائص المرتبطة بالـ QML مباشرة (Data-Binding)
    Q_PROPERTY(bool isPlaying READ isPlaying NOTIFY playingChanged)
    Q_PROPERTY(double duration READ duration NOTIFY durationChanged)
    Q_PROPERTY(double position READ position WRITE seek NOTIFY positionChanged)
    Q_PROPERTY(double volume READ volume WRITE setVolume NOTIFY volumeChanged)
    Q_PROPERTY(bool isMuted READ isMuted WRITE setMuted NOTIFY mutedChanged)
    Q_PROPERTY(QString mediaTitle READ mediaTitle NOTIFY mediaTitleChanged)
    Q_PROPERTY(bool isHDR READ isHDR NOTIFY hdrChanged)
    Q_PROPERTY(int videoWidth READ videoWidth NOTIFY videoSizeChanged)
    Q_PROPERTY(int videoHeight READ videoHeight NOTIFY videoSizeChanged)
    Q_PROPERTY(QStringList playlist READ playlist NOTIFY playlistChanged)
    Q_PROPERTY(int currentTrackIndex READ currentTrackIndex NOTIFY currentTrackIndexChanged)

public:
    explicit PlayerController(QObject* parent = nullptr);
    ~PlayerController() override;

    // دوال القراءة للـ Properties
    bool isPlaying() const { return m_isPlaying; }
    double duration() const { return m_duration; }
    double position() const { return m_position; }
    double volume() const { return m_volume; }
    bool isMuted() const { return m_isMuted; }
    QString mediaTitle() const { return m_mediaTitle; }
    bool isHDR() const { return m_isHDR; }
    int videoWidth() const { return m_videoWidth; }
    int videoHeight() const { return m_videoHeight; }
    QStringList playlist() const { return m_playlist; }
    int currentTrackIndex() const { return m_currentTrackIndex; }

    // دوال قابلة للاستدعاء مباشرة من الـ QML (Q_INVOKABLE)
    Q_INVOKABLE void openFile(const QString& filePath);
    Q_INVOKABLE void openFolder(const QString& folderPath);
    Q_INVOKABLE void togglePlay();
    Q_INVOKABLE void play();
    Q_INVOKABLE void pause();
    Q_INVOKABLE void stop();
    Q_INVOKABLE void seek(double seconds);
    Q_INVOKABLE void forward10();
    Q_INVOKABLE void backward10();
    Q_INVOKABLE void setVolume(double vol);
    Q_INVOKABLE void setMuted(bool mute);
    Q_INVOKABLE void setPlaybackRate(double rate);
    
    // إدارة قائمة التشغيل (Playlist Management)
    Q_INVOKABLE void addToPlaylist(const QString& item);
    Q_INVOKABLE void removeFromPlaylist(int index);
    Q_INVOKABLE void playPlaylistItem(int index);
    Q_INVOKABLE void playNext();
    Q_INVOKABLE void playPrevious();
    Q_INVOKABLE void clearPlaylist();

signals:
    void playingChanged();
    void durationChanged();
    void positionChanged();
    void volumeChanged();
    void mutedChanged();
    void mediaTitleChanged();
    void hdrChanged();
    void videoSizeChanged();
    void playlistChanged();
    void currentTrackIndexChanged();
    void frameReady(const QImage& frame);

private:
    void playbackLoop();
    void stopPlaybackThreads();

    std::shared_ptr<ClockSync> m_clockSync;
    std::unique_ptr<Demuxer> m_demuxer;
    std::unique_ptr<VideoDecoder> m_videoDecoder;
    std::unique_ptr<AudioDecoder> m_audioDecoder;
    std::unique_ptr<AudioEngine> m_audioEngine;

    std::jthread m_videoThread;
    std::jthread m_audioThread;
    std::atomic<bool> m_threadsRunning{false};

    bool m_isPlaying{false};
    double m_duration{0.0};
    double m_position{0.0};
    double m_volume{0.8};
    bool m_isMuted{false};
    QString m_mediaTitle;
    bool m_isHDR{false};
    int m_videoWidth{0};
    int m_videoHeight{0};

    QStringList m_playlist;
    int m_currentTrackIndex{-1};
};

} // namespace Tavrynexo::Core
