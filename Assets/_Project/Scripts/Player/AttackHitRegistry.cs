using System.Collections.Generic;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 记录一次挥剑已经接触的目标，确保多帧攻击窗口不会重复结算伤害。
    /// </summary>
    internal sealed class AttackHitRegistry
    {
        private readonly HashSet<object> targets = new();

        public bool TryRegister(object target)
        {
            return target != null && targets.Add(target);
        }

        public void Clear()
        {
            targets.Clear();
        }
    }
}
