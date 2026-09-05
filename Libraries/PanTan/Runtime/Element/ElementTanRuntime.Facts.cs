using Pan.HighDensityElement;
using UnityEngine;



namespace Pan.Tan.Element
{
    /// <summary>
    /// HDE의 원시 Fact를 backend 중립 TanFact와 접촉 geometry로 변환합니다.
    /// </summary>
    public sealed partial class ElementTanRuntime
    {
        internal void OnElementFact(in ElementFact fact)
        {
            TanKey key = ToTanKey(fact.Element);
            if (!records.TryGetValue(key, out TanRecord record)) { return; }

            if (fact.Type == ElementFactType.Contact)
            {
                DispatchContact(in key, in record, in fact);
                return;
            }

            if (fact.Type == ElementFactType.LifetimeExpired)
            {
                RaiseFact(new TanFact(
                    TanFactType.LifetimeExpired,
                    in key,
                    default,
                    new Vector2(fact.Position.x, fact.Position.y),
                    fixedStepIndex: fact.FixedStepIndex,
                    substepIndex: fact.SubstepIndex));
                return;
            }

            if (fact.Type == ElementFactType.BoundaryExited)
            {
                RaiseFact(new TanFact(
                    TanFactType.BoundaryExited,
                    in key,
                    default,
                    new Vector2(fact.Position.x, fact.Position.y),
                    fixedStepIndex: fact.FixedStepIndex,
                    substepIndex: fact.SubstepIndex));
                return;
            }

            if (fact.Type != ElementFactType.Despawned) { return; }

            ReturnContactList(record.PreviousStepContacts);
            ReturnContactList(record.CurrentStepContacts);
            records.Remove(key);
            RaiseFact(new TanFact(
                TanFactType.Despawned,
                in key,
                default,
                new Vector2(fact.Position.x, fact.Position.y),
                fixedStepIndex: fact.FixedStepIndex,
                substepIndex: fact.SubstepIndex));
        }



        private void DispatchContact(in TanKey key, in TanRecord record, in ElementFact fact)
        {
            if (targetResolver == null ||
                !targetResolver.TryResolveTarget(in fact, out TanTargetHandle target) ||
                !TryRecordContactEpisode(in key, in target))
            {
                return;
            }

            Vector2 startPosition = default;
            Vector2 endPosition = default;
            bool hasTanMotion = record.Element.TryGetSnapshot(out ElementSnapshot snapshot);
            if (hasTanMotion)
            {
                startPosition = new Vector2(snapshot.PreviousPosition.x, snapshot.PreviousPosition.y);
                endPosition = new Vector2(snapshot.Position.x, snapshot.Position.y);
            }

            Vector2 incomingDisplacement = hasTanMotion
                ? TanContactMath.CalculateIncomingDisplacement(startPosition, endPosition)
                : default;
            Vector2 centerAtImpact = fact.HasImpactCenter
                ? new Vector2(fact.ImpactCenter.x, fact.ImpactCenter.y)
                : hasTanMotion
                    ? TanContactMath.CalculateCenterAtImpact(startPosition, endPosition, fact.TimeOfImpact)
                    : new Vector2(fact.Position.x, fact.Position.y);
            Vector2 surfacePoint = fact.HasSurfacePoint
                ? new Vector2(fact.Position.x, fact.Position.y)
                : centerAtImpact;
            Vector2 normal = new Vector2(fact.Normal.x, fact.Normal.y);
            if (fact.HasSurfaceNormal) { normal.Normalize(); }
            else { normal = default; }

            TanContactGeometryFlags flags = TanContactGeometryFlags.None;
            if (fact.HasSurfacePoint) { flags |= TanContactGeometryFlags.SurfacePointValid; }
            if (fact.HasSurfaceNormal) { flags |= TanContactGeometryFlags.NormalValid; }
            if (hasTanMotion) { flags |= TanContactGeometryFlags.IncomingDisplacementValid; }
            if (fact.HasImpactCenter || hasTanMotion) { flags |= TanContactGeometryFlags.ImpactCenterValid; }
            if (fact.StartedOverlapped) { flags |= TanContactGeometryFlags.StartedOverlapped; }

            var geometry = new TanContactGeometry(
                surfacePoint,
                centerAtImpact,
                hasTanMotion ? endPosition : centerAtImpact,
                normal,
                incomingDisplacement,
                fact.TimeOfImpact,
                flags);
            RaiseFact(TanFact.CreateContact(
                in key,
                in target,
                in geometry,
                fact.FixedStepIndex,
                fact.SubstepIndex));
        }



        private void RaiseFact(in TanFact fact) => FactRaised?.Invoke(in fact);
    }
}
