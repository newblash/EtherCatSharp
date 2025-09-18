using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherCatSharp
{
    internal interface IEtherCatCoreInterfaces
    {
        /// <summary>
        /// 初始化 EtherCAT 接口
        /// </summary>
        /// <param name="ifaceName">网卡接口名称</param>
        /// <returns></returns>
        public int EtherCat_Init(string ifaceName);
        /// <summary>
        /// 初始化 EtherCAT 配置
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int EtherCat_ConfigInit(int context);
        /// <summary>
        /// EtherCAT设备配置映射。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="iosize"></param>
        /// <returns></returns>
        public int EtherCat_ConfigMap(int context, int iosize);
        /// <summary>
        /// 扫描 EtherCAT 总线上的从站设备
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="reqstate"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public ushort EtherCat_StateCheck(int context, ushort slave, SlaveState reqstate, int timeout = 8000000);
        /// <summary>
        /// 读取 EtherCAT 状态
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int EtherCat_ReadState(int context);
        /// <summary>
        /// 写入 EtherCAT 状态
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <returns></returns>
        public int EtherCat_WriteState(int context, ushort slave);
        /// <summary>
        /// 发送和接收 EtherCAT 过程数据
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int EtherCat_SendProcessData(int context);
        /// <summary>
        /// 接收 EtherCAT 过程数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_ReceiveProcessData(int context, int timeout = 2000);
        /// <summary>
        /// 写入 EtherCAT SDO（服务数据对象）数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <param name="ca"></param>
        /// <param name="psize"></param>
        /// <param name="p"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_SDOwrite(int context, ushort slave, ushort index, byte subIndex, bool ca, int psize, byte[] p, int timeout = 700000);
        /// <summary>
        /// 读取 EtherCAT SDO（服务数据对象）数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="index"></param>
        /// <param name="subindex"></param>
        /// <param name="CA"></param>
        /// <param name="psize"></param>
        /// <param name="p"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_SDOread(int context, ushort slave, ushort index, byte subindex, bool CA, ref int psize, byte[] p, int timeout = 700000);
        /// <summary>
        /// 获取EtherCAT从站的输入字节数。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <returns></returns>
        public uint Get_EtherCat_SlaveIbytes(int context, ushort slave);
        /// <summary>
        /// 获取EtherCAT从站的输出字节数。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <returns></returns>
        public uint Get_EtherCat_SlaveObytes(int context, ushort slave);
        /// <summary>
        /// 获取EtherCAT从站的输入数据。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public uint Get_EtherCat_SlaveInputs(int context, ushort slave, byte[] buf);
        /// <summary>
        /// 获取EtherCAT从站的输出数据。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public uint Get_EtherCat_SlaveOutputs(int context, ushort slave, byte[] buf);
        /// <summary>
        /// 设置EtherCAT从站的输出数据。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public uint Set_EtherCat_SlaveOutputs(int context, ushort slave, byte[] buf);
        /// <summary>
        /// 关闭 EtherCAT设备
        /// </summary>
        /// <param name="context"></param>
        public void EtherCat_Close(int context);
        /// <summary>
        /// 重新配置 EtherCAT 从站设备
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_ReconfigSlave(int context, ushort slave, int timeout);
        /// <summary>
        /// 获取 EtherCAT 从站的状态
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <returns></returns>
        public SlaveState Get_EtherCat_SlaveState(int context, ushort slave);
        /// <summary>
        /// 获取 EtherCAT 总线上从站的数量
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int Get_EtherCat_SlaveCount(int context);
        /// <summary>
        /// 设置 EtherCAT 从站的状态
        /// </summary>
        /// <param name="context"></param>
        /// <param name="slave"></param>
        /// <param name="state"></param>
        public void Set_EtherCat_SlaveState(int context, ushort slave, SlaveState state);
        /// <summary>
        /// 获取 EtherCAT 从站的名称
        /// </summary>
        /// <param name="contextt"></param>
        /// <param name="slave"></param>
        /// <param name="buf"></param>
        public void Get_EtherCat_SlaveName(int contextt, ushort slave, byte[] buf);
        /// <summary>
        /// 获取 EtherCAT 从站的配置地址
        /// </summary>
        /// <param name="contextt"></param>
        /// <param name="slave"></param>
        /// <returns></returns>
        public ushort Get_EtherCat_SlaveConfigadr(int contextt, ushort slave);
        /// <summary>
        /// 读取 EtherCAT 从站的配置数据
        /// </summary>
        /// <param name="contextt"></param>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_FPRD(int contextt, ushort ADP, ushort ADO, ushort length, byte[] data, int timeout);
        /// <summary>
        /// 写入 EtherCAT 从站的配置数据
        /// </summary>
        /// <param name="contextt"></param>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_FPWR(int contextt, ushort ADP, ushort ADO, ushort length, byte[] data, int timeout);
        /// <summary>
        /// 读取 EtherCAT 从站的应用数据
        /// </summary>
        /// <param name="contextt"></param>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_APRD(int contextt, ushort ADP, ushort ADO, ushort length, byte[] data, int timeout);
        /// <summary>
        /// 写入 EtherCAT 从站的应用数据
        /// </summary>
        /// <param name="contextt"></param>
        /// <param name="ADP"></param>
        /// <param name="ADO"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int EtherCat_APWR(int contextt, ushort ADP, ushort ADO, ushort length, byte[] data, int timeout);

    }
}
