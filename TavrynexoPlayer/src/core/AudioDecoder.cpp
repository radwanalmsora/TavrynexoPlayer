#include "core/AudioDecoder.hpp"
#include <iostream>

namespace Tavrynexo::Core {

AudioDecoder::AudioDecoder() {
    m_avFrame = av_frame_alloc();
}

AudioDecoder::~AudioDecoder() {
    close();
    if (m_avFrame) av_frame_free(&m_avFrame);
}

bool AudioDecoder::init(AVCodecParameters* codecParams, AVRational timeBase) {
    close();
    m_timeBase = timeBase;

    const AVCodec* decoder = avcodec_find_decoder(codecParams->codec_id);
    if (!decoder) {
        std::cerr << "[Tavrynexo::AudioDecoder] Unsupported audio codec" << std::endl;
        return false;
    }

    m_codecContext = avcodec_alloc_context3(decoder);
    if (!m_codecContext) return false;

    if (avcodec_parameters_to_context(m_codecContext, codecParams) < 0) {
        close();
        return false;
    }

    if (avcodec_open2(m_codecContext, decoder, nullptr) < 0) {
        close();
        return false;
    }

    // تهيئة محول العينات SwrContext لتحويل أي مسار صوتي إلى 48000Hz Stereo Float32
    AVChannelLayout outLayout;
    av_channel_layout_default(&outLayout, TARGET_CHANNELS);

    int ret = swr_alloc_set_opts2(
        &m_swrContext,
        &outLayout,
        AV_SAMPLE_FMT_FLT,
        TARGET_SAMPLE_RATE,
        &m_codecContext->ch_layout,
        m_codecContext->sample_fmt,
        m_codecContext->sample_rate,
        0, nullptr
    );

    av_channel_layout_uninit(&outLayout);

    if (ret < 0 || swr_init(m_swrContext) < 0) {
        std::cerr << "[Tavrynexo::AudioDecoder] Failed to initialize SwrContext" << std::endl;
        close();
        return false;
    }

    return true;
}

void AudioDecoder::flush() {
    if (m_codecContext) {
        avcodec_flush_buffers(m_codecContext);
    }
}

void AudioDecoder::close() {
    if (m_swrContext) {
        swr_free(&m_swrContext);
        m_swrContext = nullptr;
    }
    if (m_codecContext) {
        avcodec_free_context(&m_codecContext);
        m_codecContext = nullptr;
    }
}

bool AudioDecoder::sendPacket(const AVPacket* packet) {
    if (!m_codecContext) return false;
    return avcodec_send_packet(m_codecContext, packet) == 0;
}

bool AudioDecoder::receiveFrame(AudioFramePCM& outFrame) {
    if (!m_codecContext || !m_swrContext) return false;

    int ret = avcodec_receive_frame(m_codecContext, m_avFrame);
    if (ret != 0) return false;

    // حساب حجم العينات بعد التحويل
    int maxOutSamples = av_rescale_rnd(
        swr_get_delay(m_swrContext, m_codecContext->sample_rate) + m_avFrame->nb_samples,
        TARGET_SAMPLE_RATE, m_codecContext->sample_rate, AV_ROUND_UP
    );

    outFrame.samples.resize(maxOutSamples * TARGET_CHANNELS);
    uint8_t* outData[1] = { reinterpret_cast<uint8_t*>(outFrame.samples.data()) };

    int convertedSamples = swr_convert(
        m_swrContext,
        outData,
        maxOutSamples,
        const_cast<const uint8_t**>(m_avFrame->extended_data),
        m_avFrame->nb_samples
    );

    if (convertedSamples < 0) return false;

    outFrame.samples.resize(convertedSamples * TARGET_CHANNELS);
    outFrame.sampleRate = TARGET_SAMPLE_RATE;
    outFrame.channels = TARGET_CHANNELS;

    int64_t pts = (m_avFrame->best_effort_timestamp != AV_NOPTS_VALUE) 
                  ? m_avFrame->best_effort_timestamp 
                  : m_avFrame->pts;
    outFrame.pts = (pts != AV_NOPTS_VALUE) ? static_cast<double>(pts) * av_q2d(m_timeBase) : 0.0;

    return true;
}

} // namespace Tavrynexo::Core
