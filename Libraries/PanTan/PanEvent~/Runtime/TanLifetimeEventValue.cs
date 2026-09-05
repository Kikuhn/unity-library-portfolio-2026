using Pan.Event;



namespace Pan.Tan.PanEvent
{
    /// <summary>
    /// 수명 값을 복제하지 않고 현재 Tan의 native/backend lifetime feature를 조작하는 EventValue adapter입니다.
    /// </summary>
    [PanEventUsageProfile(PanEventUsageProfile.Runtime)]
    public sealed class TanLifetimeEventValue : PanBaseEventValue.EventAbles<TanLifetimeEventValue>
    {
        private bool removeLifetimeOnDisable;



        public bool RemoveLifetimeOnDisable => removeLifetimeOnDisable;



        public bool Configure(in TanLifetimeDefinition lifetime, bool removeOnDisable = false)
        {
            if (!lifetime.Enabled || Current is not IEventTan owner) { return false; }
            var command = new ConfigureTanLifetime(in lifetime);
            if (!owner.Tan.TrySubmit(in command)) { return false; }
            removeLifetimeOnDisable = removeOnDisable;
            return true;
        }

        public bool TryGetRemainingUnits(out float remainingUnits)
        {
            if (Current is IEventTan owner &&
                owner.Tan.TryGetSnapshot(out TanSnapshot snapshot) &&
                snapshot.RemainingLifetime.Enabled)
            {
                remainingUnits = snapshot.RemainingLifetime.Value;
                return true;
            }

            remainingUnits = 0f;
            return false;
        }

        public bool SetRemainingUnits(float remainingUnits)
        {
            if (Current is not IEventTan owner) { return false; }
            var command = new SetTanRemainingLifetime(remainingUnits);
            return owner.Tan.TrySubmit(in command);
        }

        public bool RemoveLifetime()
        {
            if (Current is not IEventTan owner) { return false; }
            var command = new RemoveTanLifetime();
            if (!owner.Tan.TrySubmit(in command)) { return false; }
            removeLifetimeOnDisable = false;
            return true;
        }



        protected override void Enable() => removeLifetimeOnDisable = false;

        protected override void Disable()
        {
            if (removeLifetimeOnDisable && Current is IEventTan owner)
            {
                var command = new RemoveTanLifetime();
                owner.Tan.TrySubmit(in command);
            }

            removeLifetimeOnDisable = false;
        }
    }
}
