#include "core/Demuxer.hpp"
#include <iostream>

namespace Tavrynexo::Core {

Demuxer::Demuxer() = default;

Demuxer::~Demuxer() {
    close();
}

bool Demuxer::open(const std::string& filePath) {
    close();
    m_currentPath = filePath;

    if (avformat_open_input(&m_formatContext, filePath.c_str(), nullptr, nullptr) < 0) {
        std::cerr << "[Tavrynexo::Demuxer] Failed to open input file: " << filePath << std::endl;
        return false;
    }

    if (avformat_find_stream_info(m_formatContext, nullptr) < 0) {
        std::cerr << "[Tavrynexo::Demuxer] Failed to retrieve stream info" << std::endl;
        close();
        return false;
    }

    m_durationSeconds = (m_formatContext->duration != AV_NOPTS_VALUE) 
                        ? static_cast<double>(m_formatContext->duration) / AV_TIME_BASE 
                        : 0.0;

    m_streams.clear();
    for (unsigned int i = 0; i < m_formatContext->nb_streams; ++i) {
        AVStream* stream = m_formatContext->streams[i];
        AVCodecParameters* codecParams = stream->codecpar;

        MediaStreamInfo info;
        info.index = i;
        info.type = codecParams->codec_type;
        const AVCodec* decoder = avcodec_find_decoder(codecParams->codec_id);
        info.codecName = decoder ? decoder->name : "unknown";

        if (codecParams->codec_type == AVMEDIA_TYPE_VIDEO && m_videoStreamIndex == -1) {
            m_videoStreamIndex = i;
            info.width = codecParams->width;
            info.height = codecParams->height;
            info.frameRate = av_q2d(stream->avg_frame_rate);
        } else if (codecParams->codec_type == AVMEDIA_TYPE_AUDIO && m_audioStreamIndex == -1) {
            m_audioStreamIndex = i;
            info.sampleRate = codecParams->sample_rate;
            info.channels = codecParams->ch_layout.nb_channels;
        } else if (codecParams->codec_type == AVMEDIA_TYPE_SUBTITLE && m_subtitleStreamIndex == -1) {
            m_subtitleStreamIndex = i;
        }

        m_streams.push_back(info);
    }

    return (m_videoStreamIndex != -1 || m_audioStreamIndex != -1);
}

void Demuxer::close() {
    stopReading();
    {
        std::lock_guard<std::mutex> lock(m_queueMutex);
        while (!m_videoQueue.empty()) {
            AVPacket* pkt = m_videoQueue.front();
            av_packet_free(&pkt);
            m_videoQueue.pop();
        }
        while (!m_audioQueue.empty()) {
            AVPacket* pkt = m_audioQueue.front();
            av_packet_free(&pkt);
            m_audioQueue.pop();
        }
    }

    if (m_formatContext) {
        avformat_close_input(&m_formatContext);
        m_formatContext = nullptr;
    }

    m_videoStreamIndex = -1;
    m_audioStreamIndex = -1;
    m_subtitleStreamIndex = -1;
    m_durationSeconds = 0.0;
}

void Demuxer::startReading() {
    if (!m_formatContext || m_isRunning.load()) return;
    m_isRunning.store(true);
    m_demuxThread = std::jthread(&Demuxer::demuxLoop, this);
}

void Demuxer::stopReading() {
    m_isRunning.store(false);
    m_queueCond.notify_all();
    if (m_demuxThread.joinable()) {
        m_demuxThread.join();
    }
}

bool Demuxer::seek(double targetSeconds) {
    if (!m_formatContext) return false;

    m_isSeeking.store(true);
    int64_t targetTimestamp = static_cast<int64_t>(targetSeconds * AV_TIME_BASE);

    int ret = avformat_seek_file(m_formatContext, -1, INT64_MIN, targetTimestamp, INT64_MAX, AVSEEK_FLAG_BACKWARD);

    {
        std::lock_guard<std::mutex> lock(m_queueMutex);
        while (!m_videoQueue.empty()) {
            AVPacket* pkt = m_videoQueue.front();
            av_packet_free(&pkt);
            m_videoQueue.pop();
        }
        while (!m_audioQueue.empty()) {
            AVPacket* pkt = m_audioQueue.front();
            av_packet_free(&pkt);
            m_audioQueue.pop();
        }
    }

    m_isSeeking.store(false);
    m_queueCond.notify_all();
    return ret >= 0;
}

void Demuxer::demuxLoop() {
    while (m_isRunning.load()) {
        if (m_isSeeking.load()) {
            std::this_thread::sleep_for(std::chrono::milliseconds(5));
            continue;
        }

        {
            std::unique_lock<std::mutex> lock(m_queueMutex);
            if (m_videoQueue.size() >= MAX_QUEUE_SIZE || m_audioQueue.size() >= MAX_QUEUE_SIZE) {
                m_queueCond.wait_for(lock, std::chrono::milliseconds(10), [this]() {
                    return !m_isRunning.load() || m_videoQueue.size() < MAX_QUEUE_SIZE;
                });
                if (!m_isRunning.load()) break;
            }
        }

        AVPacket* packet = av_packet_alloc();
        int ret = av_read_frame(m_formatContext, packet);
        if (ret < 0) {
            av_packet_free(&packet);
            // نهاية الملف (EOF)
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
            continue;
        }

        std::lock_guard<std::mutex> lock(m_queueMutex);
        if (packet->stream_index == m_videoStreamIndex) {
            m_videoQueue.push(packet);
            m_queueCond.notify_one();
        } else if (packet->stream_index == m_audioStreamIndex) {
            m_audioQueue.push(packet);
            m_queueCond.notify_one();
        } else {
            av_packet_free(&packet);
        }
    }
}

bool Demuxer::popVideoPacket(AVPacket* outPacket) {
    std::lock_guard<std::mutex> lock(m_queueMutex);
    if (m_videoQueue.empty()) return false;
    AVPacket* pkt = m_videoQueue.front();
    m_videoQueue.pop();
    av_packet_ref(outPacket, pkt);
    av_packet_free(&pkt);
    m_queueCond.notify_one();
    return true;
}

bool Demuxer::popAudioPacket(AVPacket* outPacket) {
    std::lock_guard<std::mutex> lock(m_queueMutex);
    if (m_audioQueue.empty()) return false;
    AVPacket* pkt = m_audioQueue.front();
    m_audioQueue.pop();
    av_packet_ref(outPacket, pkt);
    av_packet_free(&pkt);
    m_queueCond.notify_one();
    return true;
}

AVCodecParameters* Demuxer::getVideoCodecParams() const {
    if (!m_formatContext || m_videoStreamIndex < 0) return nullptr;
    return m_formatContext->streams[m_videoStreamIndex]->codecpar;
}

AVCodecParameters* Demuxer::getAudioCodecParams() const {
    if (!m_formatContext || m_audioStreamIndex < 0) return nullptr;
    return m_formatContext->streams[m_audioStreamIndex]->codecpar;
}

AVRational Demuxer::getVideoTimeBase() const {
    if (!m_formatContext || m_videoStreamIndex < 0) return AVRational{1, 1000};
    return m_formatContext->streams[m_videoStreamIndex]->time_base;
}

AVRational Demuxer::getAudioTimeBase() const {
    if (!m_formatContext || m_audioStreamIndex < 0) return AVRational{1, 1000};
    return m_formatContext->streams[m_audioStreamIndex]->time_base;
}

} // namespace Tavrynexo::Core
