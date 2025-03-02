using System;
using UnityEditorInternal;
using UnityEngine;

public interface ITimerHandler
{
    event Action<float> onUpdate;

    float Time { get; }
}

public class DefaultTimerHandler : MonoBehaviour, ITimerHandler
{
    enum UpdateTiming
    {
        Update,
        LateUpdate,
        FixedUpdate
    }

    [SerializeField]
    private UpdateTiming timing;

    public float Time => UnityEngine.Time.time;

    public event Action<float> onUpdate;

    public static DefaultTimerHandler FindOrCreate()
    {
        DefaultTimerHandler handler = FindFirstObjectByType<DefaultTimerHandler>();
        if (handler == null)
        {
            handler = new GameObject(nameof(DefaultTimerHandler)).AddComponent<DefaultTimerHandler>();
        }

        return handler;
    }

    private void Update()
    {
        switch (timing)
        {
            case UpdateTiming.Update:
                onUpdate?.Invoke(UnityEngine.Time.deltaTime);
                break;
        }
    }

    private void LateUpdate()
    {
        switch (timing)
        {
            case UpdateTiming.LateUpdate:
                onUpdate?.Invoke(UnityEngine.Time.deltaTime);
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (timing)
        {
            case UpdateTiming.FixedUpdate:
                onUpdate?.Invoke(UnityEngine.Time.fixedDeltaTime);
                break;
        }
    }
}