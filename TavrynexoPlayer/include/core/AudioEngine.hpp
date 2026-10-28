#pragma once

#include <vector>
#include <deque>
#include <mutex>
#include <atomic>
#include <memory>
#include "core/ClockSync.hpp"

// استدعاء miniaudio
#include "core/miniaudio.h"

namespace Tavrynexo::Core {

class AudioEngine {
public:
    explicit AudioEngine(std::shared_ptr<ClockSync> clockSync);
    ~AudioEngine();

    bool init();
    void start();
    void pause();
    void stop();
    void clearBuffer();

    void pushSamples(const std::vector<float>& samples, double pts);
    void setVolume(float volume); // 0.0f إلى 1.0f (أو أكثر للتضخيم)
    float getVolume() const { return m_volume.load(); }

    void setMuted(bool muted) { m_isMuted.store(muted); }
    bool isMuted() const { return m_isMuted.load(); }

private:
    static void dataCallback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount);

    ma_device m_device;
    ma_device_config m_deviceConfig;
    bool m_isInitialized{false};

    std::shared_ptr<ClockSync> m_clockSync;
    std::atomic<float> m_volume{0.8f};
    std::atomic<bool> m_isMuted{false};

    // Buffer متزامن للعينات الصوتية
    std::deque<float> m_audioBuffer;
    std::deque<std::pair<size_t, double>> m_ptsMarks; // لتحديث ساعة PTS في الوقت الفعلي
    std::mutex m_bufferMutex;
};

} // namespace Tavrynexo::Core
