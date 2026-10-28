#include "core/VideoDecoder.hpp"
#include <iostream>

namespace Tavrynexo::Core {

VideoDecoder::VideoDecoder() {
    m_avFrame = av_frame_alloc();
    m_rgbFrame = av_frame_alloc();
}

VideoDecoder::~VideoDecoder() {
    close();
    if (m_avFrame) av_frame_free(&m_avFrame);
    if (m_rgbFrame) av_frame_free(&m_rgbFrame);
}

bool VideoDecoder::init(AVCodecParameters* codecParams, AVRational timeBase) {
    close();
    m_timeBase = timeBase;

    const AVCodec* decoder = avcodec_find_decoder(codecParams->codec_id);
    if (!decoder) {
        std::cerr << "[Tavrynexo::VideoDecoder] Unsupported video codec" << std::endl;
        return false;
    }

    m_codecContext = avcodec_alloc_context3(decoder);
    if (!m_codecContext) return false;

    if (avcodec_parameters_to_context(m_codecContext, codecParams) < 0) {
        close();
        return false;
    }

    // تفعيل خيوط فك التشفير المتعددة (Multi-threaded decoding لسرعة 4K)
    m_codecContext->thread_count = 0; // تلقائي حسب أنوية المعالج
    if (decoder->capabilities & AV_CODEC_CAP_FRAME_THREADS) {
        m_codecContext->thread_type = FF_THREAD_FRAME;
    } else if (decoder->capabilities & AV_CODEC_CAP_SLICE_THREADS) {
        m_codecContext->thread_type = FF_THREAD_SLICE;
    }

    if (avcodec_open2(m_codecContext, decoder, nullptr) < 0) {
        close();
        return false;
    }

    m_width = m_codecContext->width;
    m_height = m_codecContext->height;

    // فحص دعم ألوان HDR (BT.2020 أو SMPTE 2084)
    m_isHDR = (m_codecContext->color_primaries == AVCOL_PRI_BT2020 ||
               m_codecContext->color_trc == AVCOL_TRC_SMPTE2084 ||
               m_codecContext->color_trc == AVCOL_TRC_ARIB_STD_B67);

    // تجهيز مخزن الإطارات بصيغة RGBA32
    int numBytes = av_image_get_buffer_size(AV_PIX_FMT_RGBA, m_width, m_height, 1);
    m_frameBuffer.resize(numBytes);
    av_image_fill_arrays(m_rgbFrame->data, m_rgbFrame->linesize, m_frameBuffer.data(),
                         AV_PIX_FMT_RGBA, m_width, m_height, 1);

    // تهيئة محول الألوان والأبعاد (SWS Scale)
    m_swsContext = sws_getContext(
        m_width, m_height, m_codecContext->pix_fmt,
        m_width, m_height, AV_PIX_FMT_RGBA,
        SWS_BILINEAR, nullptr, nullptr, nullptr
    );

    return true;
}

void VideoDecoder::flush() {
    if (m_codecContext) {
        avcodec_flush_buffers(m_codecContext);
    }
}

void VideoDecoder::close() {
    if (m_swsContext) {
        sws_freeContext(m_swsContext);
        m_swsContext = nullptr;
    }
    if (m_codecContext) {
        avcodec_free_context(&m_codecContext);
        m_codecContext = nullptr;
    }
}

bool VideoDecoder::sendPacket(const AVPacket* packet) {
    if (!m_codecContext) return false;
    return avcodec_send_packet(m_codecContext, packet) == 0;
}

bool VideoDecoder::receiveFrame(VideoFrameRGB& outFrame) {
    if (!m_codecContext || !m_swsContext) return false;

    int ret = avcodec_receive_frame(m_codecContext, m_avFrame);
    if (ret != 0) return false;

    // تحويل إطار الفيديو الخام (YUV) إلى RGBA
    sws_scale(m_swsContext, m_avFrame->data, m_avFrame->linesize, 0, m_height,
              m_rgbFrame->data, m_rgbFrame->linesize);

    outFrame.width = m_width;
    outFrame.height = m_height;
    outFrame.isHDR = m_isHDR;
    
    // حساب الـ PTS بالثواني
    int64_t pts = (m_avFrame->best_effort_timestamp != AV_NOPTS_VALUE) 
                  ? m_avFrame->best_effort_timestamp 
                  : m_avFrame->pts;
    outFrame.pts = (pts != AV_NOPTS_VALUE) ? static_cast<double>(pts) * av_q2d(m_timeBase) : 0.0;

    outFrame.data.assign(m_frameBuffer.begin(), m_frameBuffer.end());
    return true;
}

} // namespace Tavrynexo::Core
