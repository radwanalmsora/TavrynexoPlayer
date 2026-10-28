#pragma once

#include <chrono>
#include <atomic>
#include <cstdint>

namespace Tavrynexo::Core {

class ClockSync {
public:
    ClockSync();
    ~ClockSync() = default;

    void start();
    void pause();
    void resume();
    void reset();
    void setSpeed(double speed);

    // تحديث ساعة الصوت المرجعية (Master Clock) بالثواني
    void updateAudioClock(double ptsInSeconds);
    
    // الحصول على الوقت الحالي للمشغل بالثواني
    double getCurrentTime() const;

    // فحص توقيت إطار الفيديو: هل يُعرض الآن أم يُؤخر أم يُسقط؟
    // يُرجع فارق التوقيت بالثواني: سالب = متأخر (Drop/Skip)، موجب = مبكر (Delay)
    double calculateVideoFrameDelay(double framePts);

    bool isPaused() const { return m_paused.load(); }
    double getSpeed() const { return m_playbackSpeed.load(); }

private:
    std::atomic<bool> m_running{false};
    std::atomic<bool> m_paused{false};
    std::atomic<double> m_playbackSpeed{1.0};
    std::atomic<double> m_audioClock{0.0};
    
    std::chrono::steady_clock::time_point m_lastAudioUpdate;
    mutable std::chrono::steady_clock::time_point m_pauseStart;
};

} // namespace Tavrynexo::Core
