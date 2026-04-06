using System.Collections;
using UnityEngine;

namespace UniBridge
{
    public class AdTimer
    {
        private readonly WaitUntil _waitPauseCondition;
        private readonly WaitForEndOfFrame _waitEndFrame;
        private readonly WaitForSecondsRealtime _waitForSecondsRealtime;
        private readonly MonoBehaviour _runner;
        private Coroutine _timerCoroutine;
        private bool _running = false;
        private bool _paused = false;
        private bool _loop = false;
        private float _currentValue = 0;
        private float _startValue = 0;

        public AdTimer(MonoBehaviour runner)
        {
            _runner = runner;
            _waitForSecondsRealtime = new WaitForSecondsRealtime(1);
            _waitPauseCondition = new WaitUntil(() => _paused == false);
            _waitEndFrame = new WaitForEndOfFrame();
        }

        public void Start(int seconds, bool loop, ITimerHandler timerHandler)
        {
            if (_running)
                return;

            _timerCoroutine = _runner.StartCoroutine(TimerCoroutine(seconds, loop, timerHandler));
        }

        public void Reset()
        {
            _currentValue = _startValue;
            Unpause();
        }

        public void Pause()
        {
            if (!_running)
                return;

            _paused = true;
        }

        public void Unpause()
        {
            _paused = false;
        }

        public void Stop()
        {
            _running = false;
            _paused = false;

            if (_timerCoroutine != null && _runner != null)
            {
                _runner.StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private IEnumerator TimerCoroutine(int seconds, bool loop, ITimerHandler timerHandler)
        {
            _loop = loop;
            _startValue = seconds;

            if (_running)
                yield break;

            _currentValue = seconds;
            _running = true;

            while (_running)
            {
                yield return _waitForSecondsRealtime;
                _currentValue -= 1;

                yield return _waitPauseCondition;
                if (_currentValue >= 0)
                {
                    yield return _waitEndFrame;
                    continue;
                }

                yield return timerHandler.Execute();

                if (_loop)
                {
                    Reset();
                }
                else
                {
                    Stop();
                    break;
                }
            }
        }
    }
}
