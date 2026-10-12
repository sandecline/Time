using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>允许攻击尝试斩毁的弹幕。</summary>
    public interface IHittableProjectile
    {
        /// <summary>尝试由指定攻击者斩毁弹幕。</summary>
        /// <param name="attacker">发起攻击的游戏对象。</param>
        /// <returns>弹幕本次是否被成功斩毁。</returns>
        bool TryDestroyByAttack(GameObject attacker);
    }
}
