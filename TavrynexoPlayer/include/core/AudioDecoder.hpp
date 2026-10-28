#pragma once

#include <vector>
#include <cstdint>

extern "C" {
#include <libavcodec/avcodec.h>
#include <libswresample/swresample.h>
#include <libavutil/opt.h>
#include <libavutil/channel_layout.h>
}

namespace Tavrynexo::Core {

struct AudioFramePCM {
    std::vector<float> samples; // 32-bit Float PCM
    int sampleRate{48000};
    int channels{2};
    double pts{0.0};
};

class AudioDecoder {
public:
    AudioDecoder();
    ~AudioDecoder();

    bool init(AVCodecParameters* codecParams, AVRational timeBase);
    void flush();
    void close();

    bool sendPacket(const AVPacket* packet);
    bool receiveFrame(AudioFramePCM& outFrame);

    int getTargetSampleRate() const { return TARGET_SAMPLE_RATE; }
    int getTargetChannels() const { return TARGET_CHANNELS; }

private:
    AVCodecContext* m_codecContext{nullptr};
    SwrContext* m_swrContext{nullptr};
    AVFrame* m_avFrame{nullptr};
    AVRational m_timeBase{1, 1000};

    static constexpr int TARGET_SAMPLE_RATE = 48000;
    static constexpr int TARGET_CHANNELS = 2;
};

} // namespace Tavrynexo::Core
