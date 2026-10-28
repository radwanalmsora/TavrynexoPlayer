#include "core/ClockSync.hpp"
#include <algorithm>

namespace Tavrynexo::Core {

ClockSync::ClockSync() 
    : m_lastAudioUpdate(std::chrono::steady_clock::now()) 
{}

void ClockSync::start() {
    m_audioClock.store(0.0);
    m_running.store(true);
    m_paused.store(false);
    m_lastAudioUpdate = std::chrono::steady_clock::now();
}

void ClockSync::pause() {
    if (!m_paused.exchange(true)) {
        m_pauseStart = std::chrono::steady_clock::now();
    }
}

void ClockSync::resume() {
    if (m_paused.exchange(false)) {
        auto now = std::chrono::steady_clock::now();
        // ضبط الفارق الزمني لتجنب قفزة الوقت بعد الاستئناف
        m_lastAudioUpdate += (now - m_pauseStart);
    }
}

void ClockSync::reset() {
    m_audioClock.store(0.0);
    m_running.store(false);
    m_paused.store(false);
    m_playbackSpeed.store(1.0);
}

void ClockSync::setSpeed(double speed) {
    if (speed > 0.1 && speed <= 4.0) {
        m_playbackSpeed.store(speed);
    }
}

void ClockSync::updateAudioClock(double ptsInSeconds) {
    m_audioClock.store(ptsInSeconds);
    m_lastAudioUpdate = std::chrono::steady_clock::now();
}

double ClockSync::getCurrentTime() const {
    if (!m_running.load()) return 0.0;
    if (m_paused.load()) return m_audioClock.load();

    auto now = std::chrono::steady_clock::now();
    std::chrono::duration<double> elapsed = now - m_lastAudioUpdate;
    return m_audioClock.load() + (elapsed.count() * m_playbackSpeed.load());
}

double ClockSync::calculateVideoFrameDelay(double framePts) {
    double masterTime = getCurrentTime();
    double diff = framePts - masterTime;
    return diff;
}

} // namespace Tavrynexo::Core
