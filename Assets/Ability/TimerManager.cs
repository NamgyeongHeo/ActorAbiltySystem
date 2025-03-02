using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public struct TimerHandle
{
    private static int maxIndex;
    private int index;

    internal static TimerHandle None
    {
        get
        {
            return new TimerHandle() { index = -1 };
        }
    }

    public bool IsValid
    {
        get
        {
            return index >= 0;
        }
    }

    internal static TimerHandle Generate()
    {
        return new TimerHandle() { index = maxIndex++ };
    }
}

internal class TimerManager : IDisposable
{
    class Timer
    {
        private TimerManager manager;
        private Action action;

        private float startTime;
        private float delay;

        private bool isRunning;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
        }

        public Timer(TimerManager manager, Action action, float delay)
        {
            this.manager = manager;
            this.action = action;
            this.delay = delay;
        }

        public void Start()
        {
            startTime = manager.Time;
            isRunning = true;
        }

        public void Update(float time, float deltaTime)
        {
            if (!isRunning)
            {
                return;
            }

            if ((time - startTime) >= delay)
            {
                action?.Invoke();
                manager.Remove(this);
            }
        }

        public void Stop(bool invokeAction)
        {
            isRunning = false;
            if (invokeAction)
            {
                action?.Invoke();
            }
        }

        public void Reset()
        {
            startTime = manager.Time;
        }

        public void SetRemainTime(float remainTime)
        {
            delay = remainTime + manager.Time - startTime;
        }

        public void SetDelay(float delay)
        {
            this.delay = delay;
        }

        public float GetRemainTime()
        {
            return delay - (manager.Time - startTime);
        }
    }

    private Dictionary<TimerHandle, Timer> timers = new Dictionary<TimerHandle, Timer>();

    private ITimerHandler timerHandler;
    public float Time
    {
        get
        {
            return timerHandler.Time;
        }
    }

    internal TimerManager(ITimerHandler timerHandler = null)
    {
        this.timerHandler = timerHandler;
        if (timerHandler == null)
        {
            this.timerHandler = DefaultTimerHandler.FindOrCreate();
        }

        this.timerHandler.onUpdate += UpdateTimers;
    }

    private void UpdateTimers(float deltaTime)
    {
        foreach (KeyValuePair<TimerHandle, Timer> pair in timers)
        {
            pair.Value.Update(timerHandler.Time, deltaTime);
        }
    }

    public TimerHandle Start(Action action, float delay)
    {
        TimerHandle handle = TimerHandle.Generate();
        Timer timer = new Timer(this, action, delay);
        timers.Add(handle, timer);

        timer.Start();

        return handle;
    }

    public void Stop(TimerHandle handle, bool invokeAction)
    {
        if (timers.Remove(handle, out Timer timer))
        {
            timer.Stop(invokeAction);
        }
    }

    private void Remove(Timer timer)
    {
        for (int i = 0; i < timers.Count; i++)
        {
            KeyValuePair<TimerHandle, Timer> pair = timers.ElementAt(i);
            if (pair.Value == timer)
            {
                timers.Remove(pair.Key);
                break;
            }
        }
    }

    public void Reset(TimerHandle handle)
    {
        if (timers.TryGetValue(handle, out Timer timer))
        {
            timer.Reset();
        }
    }

    public float GetRemainTime(TimerHandle handle)
    {
        if (!timers.TryGetValue(handle, out Timer timer))
        {
            return 0f;
        }

        return timer.GetRemainTime();
    }

    public void SetRemainTime(TimerHandle handle, float remainTime)
    {
        if (timers.TryGetValue(handle, out Timer timer))
        {
            timer.SetRemainTime(remainTime);
        }
    }

    public void SetDelay(TimerHandle handle, float delay)
    {
        if (timers.TryGetValue(handle, out Timer timer))
        {
            timer.SetDelay(delay);
        }
    }

    public void Dispose()
    {
        foreach (KeyValuePair<TimerHandle, Timer> pair in timers)
        {
            pair.Value?.Stop(false);
        }

        timers.Clear();
    }
}