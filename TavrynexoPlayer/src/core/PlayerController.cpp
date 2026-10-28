#include "core/PlayerController.hpp"
#include <QFileInfo>
#include <QDir>
#include <algorithm>
#include <iostream>

namespace Tavrynexo::Core {

PlayerController::PlayerController(QObject* parent)
    : QObject(parent),
      m_clockSync(std::make_shared<ClockSync>()),
      m_demuxer(std::make_unique<Demuxer>()),
      m_videoDecoder(std::make_unique<VideoDecoder>()),
      m_audioDecoder(std::make_unique<AudioDecoder>()),
      m_audioEngine(std::make_unique<AudioEngine>(m_clockSync))
{
    m_audioEngine->init();
    m_audioEngine->setVolume(static_cast<float>(m_volume));
}

PlayerController::~PlayerController() {
    stop();
}

void PlayerController::openFile(const QString& filePath) {
    QString cleanPath = filePath;
    if (cleanPath.startsWith("file://")) {
        cleanPath = cleanPath.mid(7);
    }
#ifdef _WIN32
    if (cleanPath.startsWith("/") && cleanPath.length() > 2 && cleanPath[2] == ':') {
        cleanPath = cleanPath.mid(1);
    }
#endif

    stop();

    if (!m_demuxer->open(cleanPath.toStdString())) {
        std::cerr << "[Tavrynexo] Failed to open: " << cleanPath.toStdString() << std::endl;
        return;
    }

    m_duration = m_demuxer->getDuration();
    emit durationChanged();

    QFileInfo info(cleanPath);
    m_mediaTitle = info.fileName();
    emit mediaTitleChanged();

    if (m_demuxer->getVideoStreamIndex() >= 0) {
        m_videoDecoder->init(m_demuxer->getVideoCodecParams(), m_demuxer->getVideoTimeBase());
        m_videoWidth = m_videoDecoder->getWidth();
        m_videoHeight = m_videoDecoder->getHeight();
        m_isHDR = m_videoDecoder->isHDRContent();
        emit videoSizeChanged();
        emit hdrChanged();
    }

    if (m_demuxer->getAudioStreamIndex() >= 0) {
        m_audioDecoder->init(m_demuxer->getAudioCodecParams(), m_demuxer->getAudioTimeBase());
    }

    // إضافة إلى قائمة التشغيل إذا لم تكن موجودة
    if (!m_playlist.contains(cleanPath)) {
        m_playlist.append(cleanPath);
        emit playlistChanged();
    }
    m_currentTrackIndex = m_playlist.indexOf(cleanPath);
    emit currentTrackIndexChanged();

    play();
}

void PlayerController::openFolder(const QString& folderPath) {
    QString cleanPath = folderPath;
    if (cleanPath.startsWith("file://")) {
        cleanPath = cleanPath.mid(7);
    }
    QDir dir(cleanPath);
    QStringList filters{"*.mp4", "*.mkv", "*.avi", "*.webm", "*.mov", "*.mp3", "*.flac", "*.wav"};
    QStringList files = dir.entryList(filters, QDir::Files, QDir::Name);

    for (const QString& file : files) {
        QString absoluteFilePath = dir.absoluteFilePath(file);
        if (!m_playlist.contains(absoluteFilePath)) {
            m_playlist.append(absoluteFilePath);
        }
    }
    emit playlistChanged();

    if (!m_playlist.isEmpty() && m_currentTrackIndex < 0) {
        playPlaylistItem(0);
    }
}

void PlayerController::play() {
    if (m_isPlaying) return;

    m_clockSync->start();
    m_demuxer->startReading();
    m_audioEngine->start();

    m_threadsRunning.store(true);

    // خيط معالجة الفيديو (Video Loop)
    m_videoThread = std::jthread([this]() {
        AVPacket packet;
        av_init_packet(&packet);
        VideoFrameRGB rgbFrame;

        while (m_threadsRunning.load()) {
            if (m_clockSync->isPaused()) {
                std::this_thread::sleep_for(std::chrono::milliseconds(10));
                continue;
            }

            if (m_demuxer->popVideoPacket(&packet)) {
                if (m_videoDecoder->sendPacket(&packet)) {
                    while (m_videoDecoder->receiveFrame(rgbFrame)) {
                        double delay = m_clockSync->calculateVideoFrameDelay(rgbFrame.pts);
                        if (delay > 0.0) {
                            int sleepMs = static_cast<int>(delay * 1000.0);
                            std::this_thread::sleep_for(std::chrono::milliseconds(std::min(sleepMs, 50)));
                        }

                        // تحويل إطار الفيديو إلى QImage وإرساله للواجهة
                        QImage img(rgbFrame.data.data(), rgbFrame.width, rgbFrame.height,
                                   rgbFrame.width * 4, QImage::Format_RGBA8888);
                        emit frameReady(img.copy());

                        m_position = rgbFrame.pts;
                        emit positionChanged();
                    }
                }
                av_packet_unref(&packet);
            } else {
                std::this_thread::sleep_for(std::chrono::milliseconds(2));
            }
        }
    });

    // خيط معالجة الصوت (Audio Loop)
    m_audioThread = std::jthread([this]() {
        AVPacket packet;
        av_init_packet(&packet);
        AudioFramePCM pcmFrame;

        while (m_threadsRunning.load()) {
            if (m_clockSync->isPaused()) {
                std::this_thread::sleep_for(std::chrono::milliseconds(10));
                continue;
            }

            if (m_demuxer->popAudioPacket(&packet)) {
                if (m_audioDecoder->sendPacket(&packet)) {
                    while (m_audioDecoder->receiveFrame(pcmFrame)) {
                        m_audioEngine->pushSamples(pcmFrame.samples, pcmFrame.pts);
                    }
                }
                av_packet_unref(&packet);
            } else {
                std::this_thread::sleep_for(std::chrono::milliseconds(2));
            }
        }
    });

    m_isPlaying = true;
    emit playingChanged();
}

void PlayerController::pause() {
    if (!m_isPlaying) return;
    m_clockSync->pause();
    m_audioEngine->pause();
    m_isPlaying = false;
    emit playingChanged();
}

void PlayerController::togglePlay() {
    if (m_isPlaying) pause(); else play();
}

void PlayerController::stop() {
    m_threadsRunning.store(false);
    if (m_videoThread.joinable()) m_videoThread.join();
    if (m_audioThread.joinable()) m_audioThread.join();

    m_demuxer->close();
    m_videoDecoder->close();
    m_audioDecoder->close();
    m_audioEngine->stop();
    m_clockSync->reset();

    m_isPlaying = false;
    m_position = 0.0;
    emit playingChanged();
    emit positionChanged();
}

void PlayerController::seek(double seconds) {
    seconds = std::clamp(seconds, 0.0, m_duration);
    m_demuxer->seek(seconds);
    m_videoDecoder->flush();
    m_audioDecoder->flush();
    m_audioEngine->clearBuffer();
    m_clockSync->updateAudioClock(seconds);
    m_position = seconds;
    emit positionChanged();
}

void PlayerController::forward10() {
    seek(m_position + 10.0);
}

void PlayerController::backward10() {
    seek(m_position - 10.0);
}

void PlayerController::setVolume(double vol) {
    m_volume = std::clamp(vol, 0.0, 1.0);
    m_audioEngine->setVolume(static_cast<float>(m_volume));
    emit volumeChanged();
}

void PlayerController::setMuted(bool mute) {
    m_isMuted = mute;
    m_audioEngine->setMuted(m_isMuted);
    emit mutedChanged();
}

void PlayerController::setPlaybackRate(double rate) {
    m_clockSync->setSpeed(rate);
}

void PlayerController::addToPlaylist(const QString& item) {
    if (!m_playlist.contains(item)) {
        m_playlist.append(item);
        emit playlistChanged();
    }
}

void PlayerController::removeFromPlaylist(int index) {
    if (index >= 0 && index < m_playlist.size()) {
        m_playlist.removeAt(index);
        emit playlistChanged();
    }
}

void PlayerController::playPlaylistItem(int index) {
    if (index >= 0 && index < m_playlist.size()) {
        m_currentTrackIndex = index;
        emit currentTrackIndexChanged();
        openFile(m_playlist[index]);
    }
}

void PlayerController::playNext() {
    if (m_playlist.isEmpty()) return;
    int next = (m_currentTrackIndex + 1) % m_playlist.size();
    playPlaylistItem(next);
}

void PlayerController::playPrevious() {
    if (m_playlist.isEmpty()) return;
    int prev = (m_currentTrackIndex - 1 + m_playlist.size()) % m_playlist.size();
    playPlaylistItem(prev);
}

void PlayerController::clearPlaylist() {
    m_playlist.clear();
    m_currentTrackIndex = -1;
    emit playlistChanged();
    emit currentTrackIndexChanged();
}

} // namespace Tavrynexo::Core
