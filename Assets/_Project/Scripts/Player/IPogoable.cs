using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 允许下劈触发反弹、但不一定承受伤害的机关接口。
    /// </summary>
    public interface IPogoable
    {
        /// <summary>检查攻击者本次下劈是否可以触发反弹。</summary>
        /// <param name="attacker">发起下劈的游戏对象。</param>
        /// <returns>是否允许触发反弹。</returns>
        bool CanPogo(GameObject attacker);
    }
}
