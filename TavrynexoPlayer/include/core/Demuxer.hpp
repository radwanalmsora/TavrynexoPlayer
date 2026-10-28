#pragma once

#include <string>
#include <memory>
#include <vector>
#include <functional>
#include <thread>
#include <atomic>
#include <queue>
#include <mutex>
#include <condition_variable>

extern "C" {
#include <libavformat/avformat.h>
#include <libavcodec/avcodec.h>
#include <libavutil/avutil.h>
}

namespace Tavrynexo::Core {

struct MediaStreamInfo {
    int index{-1};
    AVMediaType type{AVMEDIA_TYPE_UNKNOWN};
    std::string codecName;
    int64_t durationSeconds{0};
    int width{0};
    int height{0};
    double frameRate{0.0};
    int sampleRate{0};
    int channels{0};
    std::string title;
    std::string language;
};

class Demuxer {
public:
    Demuxer();
    ~Demuxer();

    bool open(const std::string& filePath);
    void close();

    void startReading();
    void stopReading();
    bool seek(double targetSeconds);

    int getVideoStreamIndex() const { return m_videoStreamIndex; }
    int getAudioStreamIndex() const { return m_audioStreamIndex; }
    int getSubtitleStreamIndex() const { return m_subtitleStreamIndex; }
    
    double getDuration() const { return m_durationSeconds; }
    const std::vector<MediaStreamInfo>& getStreams() const { return m_streams; }

    AVCodecParameters* getVideoCodecParams() const;
    AVCodecParameters* getAudioCodecParams() const;
    AVRational getVideoTimeBase() const;
    AVRational getAudioTimeBase() const;

    // استخراج الحزم من الطابور
    bool popVideoPacket(AVPacket* outPacket);
    bool popAudioPacket(AVPacket* outPacket);

private:
    void demuxLoop();

    AVFormatContext* m_formatContext{nullptr};
    std::string m_currentPath;
    double m_durationSeconds{0.0};

    int m_videoStreamIndex{-1};
    int m_audioStreamIndex{-1};
    int m_subtitleStreamIndex{-1};

    std::vector<MediaStreamInfo> m_streams;

    std::atomic<bool> m_isRunning{false};
    std::atomic<bool> m_isSeeking{false};
    std::jthread m_demuxThread;

    // طوابير الحزم المتزامنة (Packet Queues)
    std::queue<AVPacket*> m_videoQueue;
    std::queue<AVPacket*> m_audioQueue;
    std::mutex m_queueMutex;
    std::condition_variable m_queueCond;
    static constexpr size_t MAX_QUEUE_SIZE = 120; // لمنع استهلاك الذاكرة العشوائي
};

} // namespace Tavrynexo::Core
