using System;
using System.Collections.Generic;

public abstract class Ability
{
    AbilityComponent ownerComponent;
    protected AbilityComponent OwnerComponent
    {
        get
        {
            return ownerComponent;
        }
    }

    protected IAbilityOwnable Owner
    {
        get
        {
            return ownerComponent.Owner;
        }
    }

    private bool isPassive;
    public bool IsPassive
    {
        get
        {
            return isPassive;
        }
        init
        {
            isPassive = value;
        }
    }

    private bool isActive;
    public bool IsActive
    {
        get
        {
            return isActive;
        }
    }

    private bool isPendingRemove;
    public bool IsPendingRemove
    {
        get
        {
            return isPendingRemove;
        }
        internal set
        {
            isPendingRemove = value;
        }
    }

    private bool disposed = false;
    public bool Disposed
    {
        get
        {
            return disposed;
        }
    }

    public Ability()
    {
    }

    internal void SetUp(AbilityComponent abilityComponent)
    {
        ownerComponent = abilityComponent;
        Init();

        if (isPassive)
        {
            ActivateInternal();
        }
    }

    protected virtual void Init()
    {
    }

    internal void DisposeInternal()
    {
        if (isActive)
        {
            CancelInternal();
        }

        Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Dispose managed memory...
            }

            // Dispose unmanaged memory...

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(true);
    }

    internal void ActivateInternal()
    {
        if (!CanActivate())
        {
            return;
        }

        if (isActive)
        {
            return;
        }

        isActive = true;
        Activate();
    }

    public virtual bool CanActivate()
    {
        return true;
    }

    protected virtual void Activate()
    {
    }

    protected void Finish()
    {
        isActive = false;
    }

    internal void CancelInternal()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;
        Cancel();
    }

    protected virtual void Cancel()
    {
    }

    protected internal virtual void OnApplyEffect(IEnumerable<ActorAttribute> affected, ActorEffect effect)
    {

    }

    protected internal virtual void OnRemoveEffect(IEnumerable<ActorAttribute> affected, ActorEffect effect)
    {

    }

    // Do not override this manually.
    protected internal virtual void ProcessActivateAbilityEvent(AbilityEvent abilityEvent)
    {
    }

    // Do not override this manually.
    protected internal virtual void ProcessCancelAbilityEvent(AbilityEvent abilityEvent)
    {
    }
}