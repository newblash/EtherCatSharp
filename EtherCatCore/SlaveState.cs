using System;
using System.Collections.Generic;
using System.Text;

namespace EtherCatSharp
{
    /// <summary>
    /// 从站状态枚举
    /// </summary>
    public enum SlaveState : ushort
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknow = 0,
        /// <summary>
        /// 初始化状态
        /// </summary>
        Init = 1,
        /// <summary>
        /// 预操作状态
        /// </summary>
        PreOperational = 2,
        /// <summary>
        /// 引导状态
        /// </summary>
        Boot = 3,
        /// <summary>
        /// 安全操作状态
        /// </summary>
        SafeOperational = 4,
        /// <summary>
        /// 操作状态
        /// </summary>
        Operational = 8,
        /// <summary>
        /// 应答状态
        /// </summary>
        Ack = 16,
        /// <summary>
        /// 初始化错误活动状态
        /// </summary>
        Init_ErrorActive = 17,
        /// <summary>
        /// 预操作错误活动状态
        /// </summary>
        PreOperational_ErrorActive = 18,
        /// <summary>
        /// 安全操作错误活动状态
        /// </summary>
        SafeOperational_ErrorActive = 20,
        /// <summary>
        /// 操作错误活动状态
        /// </summary>
        Operational_ErrorActive = 24
    }
}
