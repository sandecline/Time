using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>命中火花、受伤边缘与局部反馈；不暂停玩家位移，也不改世界时间。</summary>
    public sealed class CombatImpactFeedback : MonoBehaviour
    {
        private HealthSystem health;
        private PlayerCombat combat;
        private bool player;
        private void Awake() { health = GetComponent<HealthSystem>(); combat = GetComponent<PlayerCombat>(); player = CompareTag("Player"); }
        private void OnEnable()
        {
            if (health != null) health.Damaged += Damaged;
            if (combat != null) combat.AttackLanded += Landed;
        }
        private void OnDisable()
        {
            if (health != null) health.Damaged -= Damaged;
            if (combat != null) combat.AttackLanded -= Landed;
        }
        private void Damaged(DamageInfo damage)
        {
            V2WorldFeedback.Emit(damage.HitPoint, player ? new Color(1f, 0.25f, 0.22f) : Color.white);
            if (player) V2WorldFeedback.PlayerHurt();
        }
        private void Landed(AttackDirection direction, GameObject target)
        {
            if (target == null) return;
            V2WorldFeedback.Emit(target.transform.position, new Color(0.45f, 0.95f, 1f));
        }
    }
}
