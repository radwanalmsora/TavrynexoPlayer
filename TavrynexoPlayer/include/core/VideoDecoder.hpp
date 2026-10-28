#pragma once

#include <memory>
#include <vector>
#include <cstdint>

extern "C" {
#include <libavcodec/avcodec.h>
#include <libswscale/swscale.h>
#include <libavutil/imgutils.h>
}

namespace Tavrynexo::Core {

struct VideoFrameRGB {
    std::vector<uint8_t> data;
    int width{0};
    int height{0};
    double pts{0.0};
    bool isHDR{false};
};

class VideoDecoder {
public:
    VideoDecoder();
    ~VideoDecoder();

    bool init(AVCodecParameters* codecParams, AVRational timeBase);
    void flush();
    void close();

    // إرسال Packet لفك الترميز واستقبال Frame
    bool sendPacket(const AVPacket* packet);
    bool receiveFrame(VideoFrameRGB& outFrame);

    int getWidth() const { return m_width; }
    int getHeight() const { return m_height; }
    bool isHDRContent() const { return m_isHDR; }

private:
    AVCodecContext* m_codecContext{nullptr};
    AVFrame* m_avFrame{nullptr};
    AVFrame* m_rgbFrame{nullptr};
    SwsContext* m_swsContext{nullptr};
    AVRational m_timeBase{1, 1000};

    int m_width{0};
    int m_height{0};
    bool m_isHDR{false};
    std::vector<uint8_t> m_frameBuffer;
};

} // namespace Tavrynexo::Core
