using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace EtherCatSharp.EtherCatCore
{
    public static class StructEX
    {
        public static byte[] ToBytes<T>(this T structData) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] bytes = new byte[size]; // 只分配一次
            MemoryMarshal.Write(bytes.AsSpan(), ref structData);
            return bytes;
        }
        /// <summary>
        /// 从 Span<byte> 中反序列化结构体
        /// </summary>
        public static T ToStruct<T>(this byte[] data, int offset = 0) where T : struct
        {
            int structSize = Marshal.SizeOf<T>();
            if (data.Length - offset < structSize)
                throw new ArgumentException($"数据源大小小于结构体大小. 结构体大小: {structSize}, 数据源大小: {data.Length - offset}.");

            return MemoryMarshal.Read<T>(data.AsSpan().Slice(offset, structSize));
        }
        public static T ToStruct<T>(this Span<byte> data, int offset = 0) where T : struct
        {
            int structSize = Marshal.SizeOf<T>();
            int available = data.Length - offset;

            if (available >= structSize)
                return MemoryMarshal.Read<T>(data.Slice(offset, structSize));

            Span<byte> buffer = available <= 256 ? stackalloc byte[structSize] : new byte[structSize];
            data.Slice(offset, available).CopyTo(buffer);
            return MemoryMarshal.Read<T>(buffer);
        }
        //public static T ToStruct<T>(this Span<byte> data, int offset = 0) where T : struct
        //{
        //    int structSize = Marshal.SizeOf<T>();
        //    if (data.Length - offset < structSize)
        //        throw new ArgumentException($"数据源大小小于结构体大小. 结构体大小: {structSize}, 数据源大小: {data.Length - offset}.");

        //    return MemoryMarshal.Read<T>(data.Slice(offset, structSize));
        //}

    }
    public partial class EtherCatCore
    {
        #region 通用数据
        /// <summary>
        /// 返回值没有返回帧数据
        /// </summary>
        public const int EC_NOFRAME = -1;/** return value no frame returned */
        /// <summary>
        /// 收到返回值未知帧数据
        /// </summary>
        public const int EC_OTHERFRAME = -2;/** return value unknown frame received */
        /// <summary>
        /// 返回值错误
        /// </summary>
        public const int EC_ERROR = -3;/** return value general error */
        /// <summary>
        /// 从站数量超过最大限制
        /// </summary>
        public const int EC_SLAVECOUNTEXCEEDED = -4;/** return value too many slaves */
        /// <summary>
        /// 返回值超时
        /// </summary>
        public const int EC_TIMEOUT = -5;/** return value request timeout */
        /// <summary>
        /// 最大 EtherCAT 帧长度（以字节为单位）
        /// </summary>
        public const int EC_MAXECATFRAME = 1518;/** maximum EtherCAT frame length in bytes */
        /// <summary>
        /// 第一个LRW帧中使用的DC数据报的大小
        /// </summary>
        public const int EC_FIRSTDCDATAGRAM = 20;/** size of DC datagram used in first LRW frame */
        /// <summary>
        /// 数据报类型
        /// </summary>
        public const int EC_ECATTYPE = 0x1000;/** datagram type EtherCAT */
        /// <summary>
        /// 最大 EtherCAT LRW 数据长度（以字节为单位）
        /// </summary>
        /* MTU - Ethernet header - length - datagram header - WCK - FCS */
        public const int EC_MAXLRWDATA = (EC_MAXECATFRAME - 14 - 2 - 10 - 2 - 4);/** maximum EtherCAT LRW frame length in bytes */
        /// <summary>
        /// 标准帧缓冲区大小（以字节为单位）
        /// </summary>
        public const int EC_BUFSIZE = EC_MAXECATFRAME;/** standard frame buffer size in bytes */
        /// <summary>
        /// 每个信道帧缓冲区数量
        /// </summary>
        public const int EC_MAXBUF = 16;//number of frame buffers per channel (tx, rx1 rx2)
        /// <summary>
        /// TX返回RX帧超时时间,单位为微秒
        /// </summary>
        public const int EC_TIMEOUTRET = 2000;//timeout value in us for tx frame to return to rx 
        /// <summary>
        /// 用于超时重试,最多3次
        /// </summary>
        public const int EC_TIMEOUTRET3 = (EC_TIMEOUTRET * 3);//timeout value in us for safe data transfer, max. triple retry
        /// <summary>
        /// 返回安全值超时时间
        /// </summary>
        public const int EC_TIMEOUTSAFE = 20000;//timeout value in us for return "safe" variant (f.e. wireless)
        /// <summary>
        /// 超时值,用于EEPROM访问
        /// </summary>
        public const int EC_TIMEOUTEEP = 20000;//timeout value in us for EEPROM access
        /// <summary>
        /// 发送邮箱周期超时时间
        /// </summary>
        public const int EC_TIMEOUTTXM = 20000;//timeout value in us for tx mailbox cycle
        /// <summary>
        /// 接收邮箱周期超时时间
        /// </summary>
        public const int EC_TIMEOUTRXM = 700000;//timeout value in us for rx mailbox cycle 
        /// <summary>
        /// 状态检查超时时间
        /// </summary>
        public const int EC_TIMEOUTSTATE = 2000000;//timeout value in us for check statechange
        /// <summary>
        /// 如果wck=0时默认重试次数
        /// </summary>
        public const int EC_DEFAULTRETRIES = 3;//default number of retries if wkc <= 0
        /// <summary>
        /// EEPROM位图大小（以位为单位）
        /// </summary>
        public const int EC_MAXEEPBITMAP = 128;//size of EEPROM bitmap cache
        /// <summary>
        /// EEPROM缓冲区大小（以字节为单位）
        /// </summary>
        public const int EC_MAXEEPBUF = EC_MAXEEPBITMAP << 5;//size of EEPROM cache buffer
        /// <summary>
        /// 默认组大小=2^x
        /// </summary>
        public const int EC_LOGGROUPOFFSET = 16;//default group size in 2^x

        public const int EC_MAXELIST = 64;//max. entries in EtherCAT error list
        public const int EC_MAXNAME = 40;//max. length of readable name in slavelist and Object Description List
        public const int EC_MAXSLAVE = 200;//max. number of slaves in array
        public const int EC_MAXGROUP = 2;//max. number of groups
        public const int EC_MAXIOSEGMENTS = 64;//max. number of IO segments per group
        public const int EC_MAXMBX = 1486;//max. mailbox size
        public const int EC_MBXPOOLSIZE = 32;//number of mailboxes in pool
        public const int EC_MAXEEPDO = 0x200;//max. eeprom PDO entries
        public const int EC_MAXSM = 8;//max. SM used
        public const int EC_MAXFMMU = 4;//max. FMMU used
        public const int EC_MAXLEN_ADAPTERNAME = 128;//max. adapter name length
        public const int EC_MAX_MAPT = 1;//define maximum number of concurrent threads in mapping
        public const int EC_MAXODLIST = 1024;//max entries in Object Description list
        public const int EC_MAXOELIST = 256;//max entries in Object Entry list
        public const int EC_SOE_MAXNAME = 60;//max. length of readable SoE name
        public const int EC_SOE_MAXMAPPING = 64;//max. number of SoE mappings


        /// <summary>
        /// EtherCAT 以太网头部结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ec_etherheadert
        {
            // 目标 MAC 地址 (3 x uint16)
            public ushort da0;
            public ushort da1;
            public ushort da2;
            // 源 MAC 地址 (3 x uint16)
            public ushort sa0;
            public ushort sa1;
            public ushort sa2;
            // 以太网类型
            public ushort etype;
        }
        /// <summary>
        /// EtherCAT 头部大小
        /// </summary>
        public const int ETH_HEADERSIZE = 14; /** ethernet header size */
        /// <summary>
        /// EtherCAT 数据报头结构体定义
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ec_comt
        {
            /// <summary>
            /// EtherCAT 数据报长度（单位：字节）
            /// </summary>
            public ushort elength;
            /// <summary>
            /// EtherCAT 命令类型
            /// </summary>
            public byte command;
            /// <summary>
            /// 索引，用于 SOEM 中 Tx 到 Rx 的重组
            /// </summary>
            public byte index;
            /// <summary>
            /// ADP（站点地址偏移）
            /// </summary>
            public ushort ADP;
            /// <summary>
            /// ADO（寄存器地址偏移）
            /// </summary>
            public ushort ADO;
            /// <summary>
            /// 数据部分长度（单位：字节）
            /// </summary>
            public ushort dlength;
            /// <summary>
            /// 中断字段，当前未使用
            /// </summary>
            public ushort irpt;
        }
        public const int EC_HEADERSIZE = 12;/** EtherCAT header size */
        /// <summary>
        /// EtherCAT 头部中 elength 字段的大小（单位：字节）
        /// </summary>
        public const int EC_ELENGTHSIZE = 2; /** size of ec_comt.elength item in EtherCAT header */
        /// <summary>
        /// EtherCAT 头部中命令字段（command）的偏移位置
        /// </summary>
        public const int EC_CMDOFFSET = EC_ELENGTHSIZE;  /** offset position of command in EtherCAT header */
        /// <summary>
        /// Datagram 中工作计数器（WKC）字段的大小（单位：字节）
        /// </summary>
        public const int EC_WKCSIZE = 2; /** size of workcounter item in EtherCAT datagram */
        /// <summary>
        /// Datagram Follows 标志位掩码（用于 ec_comt.dlength 字段）
        /// 表示后续存在数据报文
        /// </summary>
        public const int EC_DATAGRAMFOLLOWS = (1 << 15);/** definition of datagram follows bit in ec_comt.dlength */

        public const ushort EC_NODEOFFSET = 0x1000;
        public const ushort EC_TEMPNODE = 0xffff;
        public const ushort ECT_ESMTRANS_IP = 0x0001;
        public const ushort ECT_ESMTRANS_PS = 0x0002;
        public const ushort ECT_ESMTRANS_PI = 0x0004;
        public const ushort ECT_ESMTRANS_SP = 0x0008;
        public const ushort ECT_ESMTRANS_SO = 0x0010;
        public const ushort ECT_ESMTRANS_SI = 0x0020;
        public const ushort ECT_ESMTRANS_OS = 0x0040;
        public const ushort ECT_ESMTRANS_OP = 0x0080;
        public const ushort ECT_ESMTRANS_OI = 0x0100;
        public const ushort ECT_ESMTRANS_IB = 0x0200;
        public const ushort ECT_ESMTRANS_BI = 0x0400;
        public const ushort ECT_ESMTRANS_II = 0x0800;
        public const ushort ECT_ESMTRANS_PP = 0x1000;
        public const ushort ECT_ESMTRANS_SS = 0x2000;
        public const ushort ECT_MBXPROT_AOE = 0x0001;
        public const ushort ECT_MBXPROT_EOE = 0x0002;
        public const ushort ECT_MBXPROT_COE = 0x0004;
        public const ushort ECT_MBXPROT_FOE = 0x0008;
        public const ushort ECT_MBXPROT_SOE = 0x0010;
        public const ushort ECT_MBXPROT_VOE = 0x0020;

        public const ushort ECT_COEDET_SDO = 0x01;
        public const ushort ECT_COEDET_SDOINFO = 0x02;
        public const ushort ECT_COEDET_PDOASSIGN = 0x04;
        public const ushort ECT_COEDET_PDOCONFIG = 0x08;
        public const ushort ECT_COEDET_UPLOAD = 0x10;
        public const ushort ECT_COEDET_SDOCA = 0x20;

        public const uint EC_SMENABLEMASK = 0xfffeffff;
        public const ushort ECT_MBXH_NONE = 0;
        public const ushort ECT_MBXH_CYCLIC = 1;
        public const ushort ECT_MBXH_LOST = 2;
        public const uint EC_LOCALDELAY = 200;

        /** standard SM0 flags configuration for mailbox slaves */
        public const int EC_DEFAULTMBXSM0 = 0x00010026;
        /** standard SM1 flags configuration for mailbox slaves */
        public const int EC_DEFAULTMBXSM1 = 0x00010022;
        /** standard SM0 flags configuration for digital output slaves */
        public const int EC_DEFAULTDOSM0 = 0x00010044;

        public const int MAX_FPRD_MULTI = 64;
        #endregion
        #region ec_err
        /// <summary>
        /// 无错误
        /// </summary>
        public const int EC_ERR_OK = 0;
        /// <summary>
        /// 库已初始化
        /// </summary>
        public const int EC_ERR_ALREADY_INITIALIZED = 1;
        /// <summary>
        /// 库未初始化
        /// </summary>
        public const int EC_ERR_NOT_INITIALIZED = 2;
        /// <summary>
        /// 函数执行过程中发生超时
        /// </summary>
        public const int EC_ERR_TIMEOUT = 3;
        /// <summary>
        /// 未找到从站设备
        /// </summary>
        public const int EC_ERR_NO_SLAVES = 4;
        /// <summary>
        /// 函数执行失败
        /// </summary>
        public const int EC_ERR_NOK = 5;
        #endregion
        #region ec_state

        /// <summary>
        /// 无有效状态。
        /// </summary>
        public const ushort EC_STATE_NONE = 0x00;
        /// <summary>
        /// 初始化状态
        /// </summary>
        public const ushort EC_STATE_INIT = 0x01;
        /// <summary>
        /// 预运行状态
        /// </summary>
        public const ushort EC_STATE_PRE_OP = 0x02;
        /// <summary>
        /// 启动状态
        /// </summary>
        public const ushort EC_STATE_BOOT = 0x03;
        /// <summary>
        /// 安全运行状态
        /// </summary>
        public const ushort EC_STATE_SAFE_OP = 0x04;
        /// <summary>
        /// 运行状态
        /// </summary>
        public const ushort EC_STATE_OPERATIONAL = 0x08;
        /// <summary>
        /// 确认错误（ACK 错误）
        /// </summary>
        public const ushort EC_STATE_ACK = 0x10;
        /// <summary>
        /// 错误状态（与 ACK 共用值）
        /// </summary>
        public const ushort EC_STATE_ERROR = 0x10;

        #endregion
        #region ec_bufstate
        /// <summary>
        /// 缓冲区为空
        /// </summary>
        public const ushort EC_BUF_EMPTY = 0x00;
        /// <summary>
        /// 缓冲区已分配
        /// </summary>
        public const ushort EC_BUF_ALLOC = 0x01;
        /// <summary>
        /// 缓冲区已传输
        /// </summary>
        public const ushort EC_BUF_TX = 0x02;
        /// <summary>
        /// 缓冲区已接收
        /// </summary>
        public const ushort EC_BUF_RCVD = 0x03;
        /// <summary>
        /// 缓冲区已完成处理
        /// </summary>
        public const ushort EC_BUF_COMPLETE = 0x04;
        #endregion
        #region  ec_datatype
        /// <summary>
        /// 布尔类型（1 bit）
        /// </summary>
        public const ushort ECT_BOOLEAN = 0x0001;
        /// <summary>
        /// 有符号 8 位整数（int8）
        /// </summary>
        public const ushort ECT_INTEGER8 = 0x0002;
        /// <summary>
        /// 有符号 16 位整数（int16）
        /// </summary>
        public const ushort ECT_INTEGER16 = 0x0003;
        /// <summary>
        /// 有符号 32 位整数（int32）
        /// </summary>
        public const ushort ECT_INTEGER32 = 0x0004;
        /// <summary>
        /// 无符号 8 位整数（uint8）
        /// </summary>
        public const ushort ECT_UNSIGNED8 = 0x0005;
        /// <summary>
        /// 无符号 16 位整数（uint16）
        /// </summary>
        public const ushort ECT_UNSIGNED16 = 0x0006;
        /// <summary>
        /// 无符号 32 位整数（uint32）
        /// </summary>
        public const ushort ECT_UNSIGNED32 = 0x0007;
        /// <summary>
        /// 32 位浮点数（float）
        /// </summary>
        public const ushort ECT_REAL32 = 0x0008;
        /// <summary>
        /// 可见字符串（Visible String）
        /// </summary>
        public const ushort ECT_VISIBLE_STRING = 0x0009;
        /// <summary>
        /// 八位字节字符串（Octet String）
        /// </summary>
        public const ushort ECT_OCTET_STRING = 0x000A;
        /// <summary>
        /// Unicode 字符串
        /// </summary>
        public const ushort ECT_UNICODE_STRING = 0x000B;
        /// <summary>
        /// 时间戳：当天时间（Time of Day）
        /// </summary>
        public const ushort ECT_TIME_OF_DAY = 0x000C;
        /// <summary>
        /// 时间差（Time Difference）
        /// </summary>
        public const ushort ECT_TIME_DIFFERENCE = 0x000D;
        /// <summary>
        /// 域类型（Domain，用于大数据传输如 EEPROM 内容）
        /// </summary>
        public const ushort ECT_DOMAIN = 0x000F;
        /// <summary>
        /// 有符号 24 位整数（int24）
        /// </summary>
        public const ushort ECT_INTEGER24 = 0x0010;
        /// <summary>
        /// 64 位浮点数（double）
        /// </summary>
        public const ushort ECT_REAL64 = 0x0011;
        /// <summary>
        /// 有符号 64 位整数（int64）
        /// </summary>
        public const ushort ECT_INTEGER64 = 0x0015;
        /// <summary>
        /// 无符号 24 位整数（uint24）
        /// </summary>
        public const ushort ECT_UNSIGNED24 = 0x0016;
        /// <summary>
        /// 无符号 64 位整数（uint64）
        /// </summary>
        public const ushort ECT_UNSIGNED64 = 0x001B;
        /// <summary>
        /// 1 位布尔值（bit1）
        /// </summary>
        public const ushort ECT_BIT1 = 0x0030;
        /// <summary>
        /// 2 位布尔值（bit2）
        /// </summary>
        public const ushort ECT_BIT2 = 0x0031;
        /// <summary>
        /// 3 位布尔值（bit3）
        /// </summary>
        public const ushort ECT_BIT3 = 0x0032;
        /// <summary>
        /// 4 位布尔值（bit4）
        /// </summary>
        public const ushort ECT_BIT4 = 0x0033;
        /// <summary>
        /// 5 位布尔值（bit5）
        /// </summary>
        public const ushort ECT_BIT5 = 0x0034;
        /// <summary>
        /// 6 位布尔值（bit6）
        /// </summary>
        public const ushort ECT_BIT6 = 0x0035;
        /// <summary>
        /// 7 位布尔值（bit7）
        /// </summary>
        public const ushort ECT_BIT7 = 0x0036;
        /// <summary>
        /// 8 位布尔值（bit8）
        /// </summary>
        public const ushort ECT_BIT8 = 0x0037;

        #endregion
        #region ec_cmdtype

        /// <summary>无操作</summary>
        public const byte EC_CMD_NOP = 0x00;
        /// <summary>自动递增读取</summary>
        public const byte EC_CMD_APRD = 0x01;
        /// <summary>自动递增写入</summary>
        public const byte EC_CMD_APWR = 0x02;
        /// <summary>自动递增读写</summary>
        public const byte EC_CMD_APRW = 0x03;
        /// <summary>配置地址读取</summary>
        public const byte EC_CMD_FPRD = 0x04;
        /// <summary>配置地址写入</summary>
        public const byte EC_CMD_FPWR = 0x05;
        /// <summary>配置地址读写</summary>
        public const byte EC_CMD_FPRW = 0x06;
        /// <summary>广播读取</summary>
        public const byte EC_CMD_BRD = 0x07;
        /// <summary>广播写入</summary>
        public const byte EC_CMD_BWR = 0x08;
        /// <summary>广播读写</summary>
        public const byte EC_CMD_BRW = 0x09;
        /// <summary>逻辑内存读取</summary>
        public const byte EC_CMD_LRD = 0x0a;
        /// <summary>逻辑内存写入</summary>
        public const byte EC_CMD_LWR = 0x0b;
        /// <summary>逻辑内存读写</summary>
        public const byte EC_CMD_LRW = 0x0c;
        /// <summary>自动递增读取+多写入</summary>
        public const byte EC_CMD_ARMW = 0x0d;
        /// <summary>配置地址读取+多写入</summary>
        public const byte EC_CMD_FRMW = 0x0e;

        #endregion
        #region ec_ecmdtype
        /// <summary>
        /// 无操作
        /// </summary>
        public const ushort EC_ECMD_NOP = 0x0000;
        /// <summary>
        /// 读取
        /// </summary>
        public const ushort EC_ECMD_READ = 0x0100;
        /// <summary>
        /// 写入
        /// </summary>
        public const ushort EC_ECMD_WRITE = 0x0201;
        /// <summary>
        /// 重载
        /// </summary>
        public const ushort EC_ECMD_RELOAD = 0x0300;
        #endregion
        #region EEprom state
        /// <summary>
        /// EEprom 状态机读取大小
        /// </summary>
        public const ushort EC_ESTAT_R64 = 0x0040;

        /// <summary>
        /// EEprom 状态机忙标志
        /// </summary>
        public const ushort EC_ESTAT_BUSY = 0x8000;

        /// <summary>
        /// EEprom 状态机错误标志掩码
        /// </summary>
        public const ushort EC_ESTAT_EMASK = 0x7800;

        /// <summary>
        /// EEprom 状态机错误确认
        /// </summary>
        public const ushort EC_ESTAT_NACK = 0x2000;

        /// <summary>
        /// SII 部分在 EEPROM 中的起始地址
        /// </summary>
        public const ushort ECT_SII_START = 0x0040;
        #endregion
        #region SII category
        /// <summary>
        /// SII 类别：字符串
        /// </summary>
        public const int ECT_SII_STRING = 10;
        /// <summary>
        /// SII 类别：通用
        /// </summary>
        public const int ECT_SII_GENERAL = 30;
        /// <summary>
        /// SII 类别：FMMU
        /// </summary>
        public const int ECT_SII_FMMU = 40;
        /// <summary>
        /// SII 类别：SM
        /// </summary>
        public const int ECT_SII_SM = 41;
        /// <summary>
        /// SII 类别：PDO
        /// </summary>
        public const int ECT_SII_PDO = 50;
        #endregion
        #region Item offsets in SII general section
        /// <summary>
        /// 制造商
        /// </summary>
        public const ushort ECT_SII_MANUF = 0x0008;
        /// <summary>
        /// 设备 ID
        /// </summary>
        public const ushort ECT_SII_ID = 0x000a;
        /// <summary>
        /// 版本号
        /// </summary>
        public const ushort ECT_SII_REV = 0x000c;
        /// <summary>
        /// 序列号
        /// </summary>
        public const ushort ECT_SII_SN = 0x000e;
        /// <summary>
        /// 引导接收邮箱地址
        /// </summary>
        public const ushort ECT_SII_BOOTRXMBX = 0x0014;
        /// <summary>
        /// 引导发送邮箱地址
        /// </summary>
        public const ushort ECT_SII_BOOTTXMBX = 0x0016;
        /// <summary>
        /// 邮箱大小
        /// </summary>
        public const ushort ECT_SII_MBXSIZE = 0x0019;
        /// <summary>
        /// 发送邮箱地址
        /// </summary>
        public const ushort ECT_SII_TXMBXADR = 0x001a;
        /// <summary>
        /// 接收邮箱地址
        /// </summary>
        public const ushort ECT_SII_RXMBXADR = 0x0018;
        /// <summary>
        /// 邮箱协议
        /// </summary>
        public const ushort ECT_SII_MBXPROTO = 0x001c;
        #endregion
        #region Mailbox types definitions
        /// <summary>
        /// 错误邮箱类型
        /// </summary>
        public const byte ECT_MBXT_ERR = 0x00;
        /// <summary>
        /// ADS over EtherCAT 邮箱类型
        /// </summary>
        public const byte ECT_MBXT_AOE = 0x01;
        /// <summary>
        /// Ethernet over EtherCAT 邮箱类型
        /// </summary>
        public const byte ECT_MBXT_EOE = 0x02;
        /// <summary>
        /// CANopen over EtherCAT 邮箱类型
        /// </summary>
        public const byte ECT_MBXT_COE = 0x03;
        /// <summary>
        /// File over EtherCAT 邮箱类型
        /// </summary>
        public const byte ECT_MBXT_FOE = 0x04;
        /// <summary>
        /// Servo over EtherCAT 邮箱类型
        /// </summary>
        public const byte ECT_MBXT_SOE = 0x05;
        /// <summary>
        /// 厂商特定 over EtherCAT 邮箱类型
        /// </summary>
        public const byte ECT_MBXT_VOE = 0x0f;
        #endregion
        #region CoE mailbox types
        /// <summary>
        /// 紧急报文
        /// </summary>
        public const byte ECT_COES_EMERGENCY = 0x01;
        /// <summary>
        /// SDO 请求
        /// </summary>
        public const byte ECT_COES_SDOREQ = 0x02;
        /// <summary>
        /// SDO 响应
        /// </summary>
        public const byte ECT_COES_SDORES = 0x03;
        /// <summary>
        /// 发送 PDO
        /// </summary>
        public const byte ECT_COES_TXPDO = 0x04;
        /// <summary>
        /// 接收 PDO
        /// </summary>
        public const byte ECT_COES_RXPDO = 0x05;
        /// <summary>
        /// 发送 PDO（请求响应）
        /// </summary>
        public const byte ECT_COES_TXPDO_RR = 0x06;
        /// <summary>
        /// 接收 PDO（请求响应）
        /// </summary>
        public const byte ECT_COES_RXPDO_RR = 0x07;
        /// <summary>
        /// SDO 信息
        /// </summary>
        public const byte ECT_COES_SDOINFO = 0x08;
        #endregion
        #region CoE SDO commands
        /// <summary>
        /// 下载初始化
        /// </summary>
        public const byte ECT_SDO_DOWN_INIT = 0x21;
        /// <summary>
        /// 下载显式
        /// </summary>
        public const byte ECT_SDO_DOWN_EXP = 0x23;
        /// <summary>
        /// 下载初始化（客户端仲裁）
        /// </summary>
        public const byte ECT_SDO_DOWN_INIT_CA = 0x31;
        /// <summary>
        /// 上载请求
        /// </summary>
        public const byte ECT_SDO_UP_REQ = 0x40;
        /// <summary>
        /// 上载请求（客户端仲裁）
        /// </summary>
        public const byte ECT_SDO_UP_REQ_CA = 0x50;
        /// <summary>
        /// 分段上载请求
        /// </summary>
        public const byte ECT_SDO_SEG_UP_REQ = 0x60;
        /// <summary>
        /// 终止传输
        /// </summary>
        public const byte ECT_SDO_ABORT = 0x80;
        #endregion
        #region CoE Object Description commands
        /// <summary>
        /// 获取对象字典列表请求
        /// </summary>
        public const byte ECT_GET_ODLIST_REQ = 0x01;
        /// <summary>
        /// 获取对象字典列表响应
        /// </summary>
        public const byte ECT_GET_ODLIST_RES = 0x02;
        /// <summary>
        /// 获取对象描述请求
        /// </summary>
        public const byte ECT_GET_OD_REQ = 0x03;
        /// <summary>
        /// 获取对象描述响应
        /// </summary>
        public const byte ECT_GET_OD_RES = 0x04;
        /// <summary>
        /// 获取对象条目请求
        /// </summary>
        public const byte ECT_GET_OE_REQ = 0x05;
        /// <summary>
        /// 获取对象条目响应
        /// </summary>
        public const byte ECT_GET_OE_RES = 0x06;
        /// <summary>
        /// SDOINFO 错误
        /// </summary>
        public const byte ECT_SDOINFO_ERROR = 0x07;
        #endregion
        #region FoE opcodes
        /// <summary>
        /// 读取
        /// </summary>
        public const byte ECT_FOE_READ = 0x01;
        /// <summary>
        /// 写入
        /// </summary>
        public const byte ECT_FOE_WRITE = 0x02;
        /// <summary>
        /// 数据
        /// </summary>
        public const byte ECT_FOE_DATA = 0x03;
        /// <summary>
        /// 确认
        /// </summary>
        public const byte ECT_FOE_ACK = 0x04;
        /// <summary>
        /// 错误
        /// </summary>
        public const byte ECT_FOE_ERROR = 0x05;
        /// <summary>
        /// 忙碌
        /// </summary>
        public const byte ECT_FOE_BUSY = 0x06;
        #endregion
        #region SoE opcodes
        /// <summary>
        /// 读取请求
        /// </summary>
        public const byte ECT_SOE_READREQ = 0x01;
        /// <summary>
        /// 读取响应
        /// </summary>
        public const byte ECT_SOE_READRES = 0x02;
        /// <summary>
        /// 写入请求
        /// </summary>
        public const byte ECT_SOE_WRITEREQ = 0x03;
        /// <summary>
        /// 写入响应
        /// </summary>
        public const byte ECT_SOE_WRITERES = 0x04;
        /// <summary>
        /// 通知
        /// </summary>
        public const byte ECT_SOE_NOTIFICATION = 0x05;
        /// <summary>
        /// 紧急事件
        /// </summary>
        public const byte ECT_SOE_EMERGENCY = 0x06;
        #endregion
        #region Ethercat registers

        public const ushort ECT_REG_TYPE = 0x0000;
        public const ushort ECT_REG_PORTDES = 0x0007;
        public const ushort ECT_REG_ESCSUP = 0x0008;
        public const ushort ECT_REG_STADR = 0x0010;
        public const ushort ECT_REG_ALIAS = 0x0012;
        public const ushort ECT_REG_DLCTL = 0x0100;
        public const ushort ECT_REG_DLPORT = 0x0101;
        public const ushort ECT_REG_DLALIAS = 0x0103;
        public const ushort ECT_REG_DLSTAT = 0x0110;
        public const ushort ECT_REG_ALCTL = 0x0120;
        public const ushort ECT_REG_ALSTAT = 0x0130;
        public const ushort ECT_REG_ALSTATCODE = 0x0134;
        public const ushort ECT_REG_PDICTL = 0x0140;
        public const ushort ECT_REG_IRQMASK = 0x0200;
        public const ushort ECT_REG_RXERR = 0x0300;
        public const ushort ECT_REG_FRXERR = 0x0308;
        public const ushort ECT_REG_EPUECNT = 0x030C;
        public const ushort ECT_REG_PECNT = 0x030D;
        public const ushort ECT_REG_PECODE = 0x030E;
        public const ushort ECT_REG_LLCNT = 0x0310;
        public const ushort ECT_REG_WDCNT = 0x0442;
        public const ushort ECT_REG_EEPCFG = 0x0500;
        public const ushort ECT_REG_EEPCTL = 0x0502;
        public const ushort ECT_REG_EEPSTAT = 0x0502;
        public const ushort ECT_REG_EEPADR = 0x0504;
        public const ushort ECT_REG_EEPDAT = 0x0508;
        public const ushort ECT_REG_FMMU0 = 0x0600;
        public const ushort ECT_REG_FMMU1 = ECT_REG_FMMU0 + 0x10;
        public const ushort ECT_REG_FMMU2 = ECT_REG_FMMU1 + 0x10;
        public const ushort ECT_REG_FMMU3 = ECT_REG_FMMU2 + 0x10;
        public const ushort ECT_REG_SM0 = 0x0800;
        public const ushort ECT_REG_SM1 = ECT_REG_SM0 + 0x08;
        public const ushort ECT_REG_SM2 = ECT_REG_SM1 + 0x08;
        public const ushort ECT_REG_SM3 = ECT_REG_SM2 + 0x08;
        public const ushort ECT_REG_SM0STAT = ECT_REG_SM0 + 0x05;
        public const ushort ECT_REG_SM1STAT = ECT_REG_SM1 + 0x05;
        public const ushort ECT_REG_SM1ACT = ECT_REG_SM1 + 0x06;
        public const ushort ECT_REG_SM1CONTR = ECT_REG_SM1 + 0x07;
        public const ushort ECT_REG_DCTIME0 = 0x0900;
        public const ushort ECT_REG_DCTIME1 = 0x0904;
        public const ushort ECT_REG_DCTIME2 = 0x0908;
        public const ushort ECT_REG_DCTIME3 = 0x090C;
        public const ushort ECT_REG_DCSYSTIME = 0x0910;
        public const ushort ECT_REG_DCSOF = 0x0918;
        public const ushort ECT_REG_DCSYSOFFSET = 0x0920;
        public const ushort ECT_REG_DCSYSDELAY = 0x0928;
        public const ushort ECT_REG_DCSYSDIFF = 0x092C;
        public const ushort ECT_REG_DCSPEEDCNT = 0x0930;
        public const ushort ECT_REG_DCTIMEFILT = 0x0934;
        public const ushort ECT_REG_DCCUC = 0x0980;
        public const ushort ECT_REG_DCSYNCACT = 0x0981;
        public const ushort ECT_REG_DCSTART0 = 0x0990;
        public const ushort ECT_REG_DCCYCLE0 = 0x09A0;
        public const ushort ECT_REG_DCCYCLE1 = 0x09A4;
        #endregion
        #region standard SDO Sync Manager
        /// <summary>
        /// 标准 SDO 同步管理器通信类型
        /// </summary>
        public const ushort ECT_SDO_SMCOMMTYPE = 0x1c00;
        /// <summary>
        /// 标准 SDO PDO 分配
        /// </summary>
        public const ushort ECT_SDO_PDOASSIGN = 0x1c10;
        /// <summary>
        /// 标准 SDO RxPDO 分配
        /// </summary>
        public const ushort ECT_SDO_RXPDOASSIGN = 0x1c12;
        /// <summary>
        /// 标准 SDO TxPDO 分配
        /// </summary>
        public const ushort ECT_SDO_TXPDOASSIGN = 0x1c13;
        /// <summary>
        /// Ethercat 数据包类型
        /// </summary>
        public const ushort ETH_P_ECAT = 0x88A4;
        #endregion
        #region Error types
        /// <summary>
        /// SDO 错误
        /// </summary>
        public const int EC_ERR_TYPE_SDO_ERROR = 0;
        /// <summary>
        /// 紧急事件
        /// </summary>
        public const int EC_ERR_TYPE_EMERGENCY = 1;
        /// <summary>
        /// 数据包错误
        /// </summary>
        public const int EC_ERR_TYPE_PACKET_ERROR = 3;
        /// <summary>
        /// SDOINFO 错误
        /// </summary>
        public const int EC_ERR_TYPE_SDOINFO_ERROR = 4;
        /// <summary>
        /// FoE 错误
        /// </summary>
        public const int EC_ERR_TYPE_FOE_ERROR = 5;
        /// <summary>
        /// FoE 缓冲区过小
        /// </summary>
        public const int EC_ERR_TYPE_FOE_BUF2SMALL = 6;
        /// <summary>
        /// FoE 包编号错误
        /// </summary>
        public const int EC_ERR_TYPE_FOE_PACKETNUMBER = 7;
        /// <summary>
        /// SoE 错误
        /// </summary>
        public const int EC_ERR_TYPE_SOE_ERROR = 8;
        /// <summary>
        /// 邮箱错误
        /// </summary>
        public const int EC_ERR_TYPE_MBX_ERROR = 9;
        /// <summary>
        /// FoE 文件未找到
        /// </summary>
        public const int EC_ERR_TYPE_FOE_FILE_NOTFOUND = 10;
        /// <summary>
        /// EOE 无效的接收数据
        /// </summary>
        public const int EC_ERR_TYPE_EOE_INVALID_RX_DATA = 11;
        #endregion

        #region Mailbox Header Macros

        /// <summary>
        /// 设置邮箱头部中的计数值 (左移 4 位)
        /// </summary>
        public static byte MBX_HDR_SET_CNT(int cnt) => (byte)(cnt << 4);

        #endregion

        #region Word / Byte Manipulation

        /// <summary>
        /// 从高位和低位字节组合成一个 word (16-bit)
        /// </summary>
        public static ushort MK_WORD(byte msb, byte lsb) =>
            (ushort)((msb << 8) | lsb);

        /// <summary>
        /// 获取 word 的高位字节
        /// </summary>
        public static byte HI_BYTE(ushort w) => (byte)(w >> 8);

        /// <summary>
        /// 获取 word 的低位字节
        /// </summary>
        public static byte LO_BYTE(ushort w) => (byte)(w & 0x00FF);

        /// <summary>
        /// 交换 word 的高低字节
        /// </summary>
        public static ushort SWAP(ushort w) =>
            (ushort)(((w & 0xFF00) >> 8) | ((w & 0x00FF) << 8));

        /// <summary>
        /// 获取 dword 的低 16 位
        /// </summary>
        public static ushort LO_WORD(uint l) => (ushort)(l & 0xFFFF);

        /// <summary>
        /// 获取 dword 的高 16 位
        /// </summary>
        public static ushort HI_WORD(uint l) => (ushort)(l >> 16);

        #endregion

        #region Endian Conversion Helpers


        /// <summary>
        /// 数组复制功能
        /// <paramref name="目标数组"/>
        /// <paramref name="目标偏移量"/>
        /// <paramref name="源数组"/>
        /// <paramref name="源偏移量"/>
        /// <paramref name="大小"/>
        /// </summary>
        /// <param name="dst">目标数组</param>
        /// <param name="dstOffset">目标偏移量</param>
        /// <param name="src">源数组</param>
        /// <param name="srcOffset">源偏移量</param>
        /// <param name="size">大小</param>
        public static void memcpy(Span<byte> dst, int dstOffset, Span<byte> src, int srcOffset, int size)
        {
            src.Slice(srcOffset, size).CopyTo(dst.Slice(dstOffset, size));
        }
        // 从 uint 数组复制到 byte 数组
        public static void memcpy(Span<byte> dst, int dstOffset, Span<uint> src, int srcOffset, int size)
        {
            //if (src.Length * sizeof(uint) > dst.Length)
                if (size > dst.Length)
                    throw new ArgumentException("Destination array is too small");

            // 将 uint span 重新解释为 byte span
            ReadOnlySpan<byte> sourceAsByte = MemoryMarshal.AsBytes(src);
            memcpy(dst, dstOffset, sourceAsByte.ToArray(), srcOffset, size);

        }
        /// <summary>
        /// 数组清0功能
        /// <paramref name="源数组"/>
        /// <paramref name="偏移量"/>
        /// <paramref name="大小"/>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="srcOffset"></param>
        /// <param name="size"></param>
        public static void memset(Span<byte> src, int srcOffset, int size)
        {
            src.Slice(srcOffset, size).Fill(0x00);
        }

        // 判断当前是否为小端序（x86/x64 通常是小端）
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;
        public static ushort htons(ushort A)
        {
            return (ushort)IPAddress.HostToNetworkOrder((short)A);
        }
        public static ushort ntohs(ushort A)
        {
            var b = (ushort)IPAddress.NetworkToHostOrder((short)A);
            return b;
        }
        // 16-bit
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ushort htoes(ushort value)
        {
#if EC_LITTLE_ENDIAN
        return value; // 小端：无需转换
#elif EC_BIG_ENDIAN
        return (ushort)(((value & 0xFF00) >> 8) |
                        ((value & 0x00FF) << 8));
#else
            return IsLittleEndian ? value :
                   (ushort)(((value & 0xFF00) >> 8) | ((value & 0x00FF) << 8));
#endif
        }

        // 32-bit
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint htoel(uint value)
        {
#if EC_LITTLE_ENDIAN
        return value;
#elif EC_BIG_ENDIAN
        return ((value & 0xFF000000) >> 24) |
               ((value & 0x00FF0000) >> 8)  |
               ((value & 0x0000FF00) << 8)  |
               ((value & 0x000000FF) << 24);
#else
            return IsLittleEndian ? value :
                   ((value & 0xFF000000) >> 24) |
                   ((value & 0x00FF0000) >> 8) |
                   ((value & 0x0000FF00) << 8) |
                   ((value & 0x000000FF) << 24);
#endif
        }

        // 64-bit
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong htoell(ulong value)
        {
#if EC_LITTLE_ENDIAN
        return value;
#elif EC_BIG_ENDIAN
        return ((value & 0xFF00000000000000UL) >> 56) |
               ((value & 0x00FF000000000000UL) >> 40) |
               ((value & 0x0000FF0000000000UL) >> 24) |
               ((value & 0x000000FF00000000UL) >> 8)  |
               ((value & 0x00000000FF000000UL) << 8)  |
               ((value & 0x0000000000FF0000UL) << 24) |
               ((value & 0x000000000000FF00UL) << 40) |
               ((value & 0x00000000000000FFUL) << 56);
#else
            return IsLittleEndian ? value :
                   ((value & 0xFF00000000000000UL) >> 56) |
                   ((value & 0x00FF000000000000UL) >> 40) |
                   ((value & 0x0000FF0000000000UL) >> 24) |
                   ((value & 0x000000FF00000000UL) >> 8) |
                   ((value & 0x00000000FF000000UL) << 8) |
                   ((value & 0x0000000000FF0000UL) << 24) |
                   ((value & 0x000000000000FF00UL) << 40) |
                   ((value & 0x00000000000000FFUL) << 56);
#endif
        }

        // etohx 是 htoex 的反向，但在你的代码中是同义词（因为是对称操作）
        public static ushort etohs(ushort value) => htoes(value);
        public static uint etohl(uint value) => htoel(value);
        public static ulong etohll(ulong value) => htoell(value);
        #endregion

        #region Unaligned Memory Access
        //public unsafe static T GetUnaligned<T>(void* ptr) where T : unmanaged
        //{
        //    T tmp = default;
        //    Buffer.MemoryCopy(ptr, &tmp, sizeof(T), sizeof(T));
        //    return tmp;
        //}
        public static void PutUnaligned32(uint value, Span<byte> buffer, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            memcpy(buffer, offset, bytes, 0, 4);
            //Buffer.BlockCopy(bytes, 0, buffer, offset * 2, 4);
        }
        public static void PutUnaligned64(ulong value, Span<byte> buffer, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            memcpy(buffer, offset, bytes, 0, 8);
            //Buffer.BlockCopy(bytes, 0, buffer, offset * 2, 8);
        }
        /// <summary>
        /// 安全读取任意地址上的 T 类型数据（非对齐访问）
        /// </summary>
        public unsafe static T GetUnaligned<T>(byte* ptr) where T : unmanaged
        {
            return *(T*)ptr;
        }

        /// <summary>
        /// 写入一个 32 位值到非对齐内存
        /// </summary>
        public unsafe static void PutUnaligned32(byte* ptr, uint val)
        {
            uint* p = (uint*)(&ptr);
            *p = val;
        }

        /// <summary>
        /// 写入一个 64 位值到非对齐内存
        /// </summary>
        public unsafe static void PutUnaligned64(byte* ptr, ulong val)
        {
            ulong* p = (ulong*)(&ptr);
            *p = val;
        }

        #endregion

        #region nicdrv.h中结构体
        // 将任意对象序列化为 byte[]
        public static byte[] ToBytes<T>(T obj) where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var type = typeof(T);
            var fields = GetOrderedFields(type); // 获取按声明顺序排列的字段

            using (var ms = new System.IO.MemoryStream())
            {
                foreach (var field in fields)
                {
                    var value = field.GetValue(obj);

                    if (field.FieldType == typeof(ushort))
                    {
                        var bytes = BitConverter.GetBytes((ushort)value);
                        ms.Write(bytes, 0, 2);
                    }
                    else if (field.FieldType == typeof(short))
                    {
                        var bytes = BitConverter.GetBytes((short)value);
                        ms.Write(bytes, 0, 2);
                    }
                    else if (field.FieldType == typeof(byte))
                    {
                        ms.WriteByte((byte)value);
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        var bytes = BitConverter.GetBytes((int)value);
                        ms.Write(bytes, 0, 4);
                    }
                    else if (field.FieldType == typeof(uint))
                    {
                        var bytes = BitConverter.GetBytes((uint)value);
                        ms.Write(bytes, 0, 4);
                    }
                    else if (field.FieldType == typeof(short))
                    {
                        var bytes = BitConverter.GetBytes((short)value);
                        ms.Write(bytes, 0, 2);
                    }
                    else if (field.FieldType == typeof(byte[]))
                    {
                        var bytes = (byte[])value;
                        ms.Write(bytes, 0, bytes.Length);
                    }
                    else if (field.FieldType == typeof(ushort[]))
                    {
                        foreach (var item in (ushort[])value)
                        {
                            var bytes = BitConverter.GetBytes(item);
                            ms.Write(bytes, 0, 2);
                        }
                    }
                    else if (field.FieldType == typeof(short[]))
                    {
                        foreach (var item in (short[])value)
                        {
                            var bytes = BitConverter.GetBytes(item);
                            ms.Write(bytes, 0, 2);
                        }
                    }
                    else if (field.FieldType == typeof(uint[]))
                    {
                        foreach (var item in (uint[])value)
                        {
                            var bytes = BitConverter.GetBytes(item);
                            ms.Write(bytes, 0, 4);
                        }

                    }
                    // 可以继续添加其他值类型...

                    else
                    {
                        throw new NotSupportedException($"不支持的字段类型: {field.FieldType}");
                    }
                }

                return ms.ToArray();
            }
        }

        // 从 byte[] 反序列化为对象
        public static T FromBytes<T>(byte[] bytes) where T : class, new()
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            var obj = new T();
            var type = typeof(T);
            var fields = GetOrderedFields(type);

            int offset = 0;
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(ushort))
                {
                    if (offset + 2 > bytes.Length) throw new InvalidOperationException("字节数组不足");
                    var value = BitConverter.ToUInt16(bytes, offset);
                    field.SetValue(obj, value);
                    offset += 2;
                }
                else if (field.FieldType == typeof(byte))
                {
                    if (offset >= bytes.Length) throw new InvalidOperationException("字节数组不足");
                    field.SetValue(obj, bytes[offset]);
                    offset += 1;
                }
                else if (field.FieldType == typeof(int))
                {
                    if (offset + 4 > bytes.Length) throw new InvalidOperationException("字节数组不足");
                    var value = BitConverter.ToInt32(bytes, offset);
                    field.SetValue(obj, value);
                    offset += 4;
                }
                else if (field.FieldType == typeof(uint))
                {
                    if (offset + 4 > bytes.Length) throw new InvalidOperationException("字节数组不足");
                    var value = BitConverter.ToUInt32(bytes, offset);
                    field.SetValue(obj, value);
                    offset += 4;
                }
                else if (field.FieldType == typeof(short))
                {
                    if (offset + 2 > bytes.Length) throw new InvalidOperationException("字节数组不足");
                    var value = BitConverter.ToInt16(bytes, offset);
                    field.SetValue(obj, value);
                    offset += 2;
                }
                else if (field.FieldType == typeof(byte[]))
                {
                    // 获取FieldLength特性
                    var fieldLengthAttr = field.GetCustomAttribute<FieldLengthAttribute>();
                    if (fieldLengthAttr == null)
                        throw new InvalidOperationException($"字段 {field.Name} 类型为byte[]，但未指定FieldLength特性");

                    int length = fieldLengthAttr.Length;
                    if (offset + length > bytes.Length)
                        throw new InvalidOperationException("字节数组不足");

                    byte[] value = new byte[length];
                    Array.Copy(bytes, offset, value, 0, length);
                    field.SetValue(obj, value);
                    offset += length;
                }
                else if (field.FieldType == typeof(ushort[]))
                {
                    // 处理 ushort[] 字段
                    int elementCount = GetArrayElementCount(field, obj, bytes, ref offset);
                    int byteLength = elementCount * sizeof(ushort);

                    if (offset + byteLength > bytes.Length)
                        throw new InvalidOperationException("字节数组不足");

                    ushort[] value = new ushort[elementCount];
                    for (int i = 0; i < elementCount; i++)
                    {
                        value[i] = BitConverter.ToUInt16(bytes, offset);
                        offset += sizeof(ushort);
                    }
                    field.SetValue(obj, value);
                }
                else if (field.FieldType == typeof(short[]))
                {
                    // 处理 ushort[] 字段
                    int elementCount = GetArrayElementCount(field, obj, bytes, ref offset);
                    int byteLength = elementCount * sizeof(short);

                    if (offset + byteLength > bytes.Length)
                        throw new InvalidOperationException("字节数组不足");

                    short[] value = new short[elementCount];
                    for (int i = 0; i < elementCount; i++)
                    {
                        value[i] = BitConverter.ToInt16(bytes, offset);
                        offset += sizeof(short);
                    }
                    field.SetValue(obj, value);
                }
                else if (field.FieldType == typeof(uint[]))
                {

                    // 处理 ushort[] 字段
                    int elementCount = GetArrayElementCount(field, obj, bytes, ref offset);
                    int byteLength = elementCount * sizeof(uint);

                    if (offset + byteLength > bytes.Length)
                        throw new InvalidOperationException("字节数组不足");

                    uint[] value = new uint[elementCount];
                    for (int i = 0; i < elementCount; i++)
                    {
                        value[i] = BitConverter.ToUInt32(bytes, offset);
                        offset += sizeof(uint);
                    }
                    field.SetValue(obj, value);
                }
                else
                {
                    throw new NotSupportedException($"不支持的字段类型: {field.FieldType}");
                }
            }

            return obj;
        }
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        public class FieldLengthAttribute : Attribute
        {
            public int Length { get; private set; }
            public string LengthField { get; private set; }

            // 固定长度
            public FieldLengthAttribute(int length)
            {
                Length = length;
            }

            // 动态长度，通过另一个字段的值确定
            public FieldLengthAttribute(string lengthField)
            {
                LengthField = lengthField;
            }
        }
        // 获取类中按声明顺序排列的字段（反射）
        private static FieldInfo[] GetOrderedFields(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       .OrderBy(f => f.MetadataToken) // MetadataToken 大致反映声明顺序
                       .ToArray();
        }
        // 获取数组元素数量（对于 ushort[] 等类型）
        private static int GetArrayElementCount(FieldInfo field, object obj, byte[] bytes, ref int offset)
        {
            // 检查是否有 FieldLength 特性
            var lengthAttr = field.GetCustomAttribute<FieldLengthAttribute>();

            if (lengthAttr != null)
            {
                // 固定元素数量
                if (lengthAttr.Length > 0)
                {
                    return lengthAttr.Length;
                }
                // 动态元素数量，通过另一个字段的值确定
                else if (!string.IsNullOrEmpty(lengthAttr.LengthField))
                {
                    var lengthField = obj.GetType().GetField(lengthAttr.LengthField,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (lengthField == null)
                        throw new InvalidOperationException($"找不到长度字段: {lengthAttr.LengthField}");

                    return Convert.ToInt32(lengthField.GetValue(obj));
                }
            }

            // 如果没有指定长度，计算剩余字节可以容纳多少个元素
            int remainingBytes = bytes.Length - offset;
            return remainingBytes / sizeof(ushort);
        }
        /// <summary>
        /// 用于获取错误的结构体
        /// </summary>
        public class ec_errort
        {
            /// <summary>
            /// 错误生成的时间
            /// </summary>
            public ec_timet Time { get; set; } = new ec_timet();
            /// <summary>
            /// 信号位，错误已生成但未读取
            /// </summary>
            public byte Signal { get; set; }
            /// <summary>
            /// 生成错误的从站编号
            /// </summary>
            public ushort Slave { get; set; }
            /// <summary>
            /// 生成错误的 CoE SDO 索引
            /// </summary>
            public ushort Index { get; set; }
            /// <summary>
            /// 生成错误的 CoE SDO 子索引
            /// </summary>
            public byte SubIdx { get; set; }
            /// <summary>
            /// 错误类型
            /// </summary>
            public int Etype { get; set; }
            public int AbortCode { get; set; }
            /// <summary>
            /// 错误代码
            /// </summary>
            public ushort ErrorCode { get; set; }

            /// <summary>
            /// 错误寄存器
            /// </summary>
            public byte ErrorReg { get; set; }

            /// <summary>
            /// 保留字段 b1
            /// </summary>
            public byte b1 { get; set; }

            /// <summary>
            /// 保留字段 w1
            /// </summary>
            public ushort w1 { get; set; }

            /// <summary>
            /// 保留字段 w2
            /// </summary>
            public ushort w2 { get; set; }
        }
        /// <summary>
        /// 时间结构体
        /// </summary>
        public class ec_timet
        {
            public uint sec { get; set; }//秒
            public uint usec { get; set; }//微秒
        }

        public class ec_stackT
        {
            /** tx buffer */
            public byte[][] txbuf { get; set; } = new byte[EC_BUFSIZE][];
            /** tx buffer lengths */
            public int[] txbuflength { get; set; } = new int[EC_MAXBUF];
            /** temporary receive buffer */
            public byte[] tempbuf { get; set; } = new byte[EC_MAXECATFRAME];
            /** rx buffers */
            public byte[][] rxbuf { get; set; } = new byte[EC_BUFSIZE][];
            /** rx buffer status fields */
            public int[] rxbufstat { get; set; } = new int[EC_MAXBUF];
            /** received MAC source address (middle word) */
            public int[] rxsa { get; set; } = new int[EC_MAXBUF];
            /** number of received frames */
            public ulong rxcnt { get; set; } = 0;

            public ec_stackT()
            {
                //C#中不能直接定义二维数组,采用在结构体中初始化的方式
                for (int i = 0; i < EC_MAXBUF; i++)
                {
                    txbuf[i] = new byte[EC_MAXECATFRAME];
                    rxbuf[i] = new byte[EC_MAXECATFRAME];
                }
            }
        }
        public class ecx_redportt
        {
            public ec_stackT stack { get; set; } = new ec_stackT();
            /** rx buffers */
            public byte[][] rxbuf { get; set; } = new byte[EC_MAXBUF][];
            /** rx buffer status */
            public int[] rxbufstat { get; set; } = new int[EC_MAXBUF];
            /** rx MAC source address */
            public int[] rxsa { get; set; } = new int[EC_MAXBUF];
            /** temporary rx buffer */
            public byte[] tempinbuf { get; set; } = new byte[EC_MAXECATFRAME];

            public ecx_redportt()
            {
                for (int i = 0; i < EC_MAXBUF; i++)
                {
                    rxbuf[i] = new byte[EC_MAXECATFRAME];
                }
            }
        }
        public class ecx_portt
        {
            public ec_stackT stack { get; set; } = new ec_stackT();
            /** rx buffers */
            public byte[][] rxbuf { get; set; } = new byte[EC_MAXBUF][];
            /** rx buffer status */
            public int[] rxbufstat { get; set; } = new int[EC_MAXBUF];
            /** rx MAC source address */
            public int[] rxsa { get; set; } = new int[EC_MAXBUF];
            /** temporary rx buffer */
            public byte[] tempinbuf { get; set; } = new byte[EC_MAXECATFRAME];
            /** temporary rx buffer status */
            public int tempinbufs { get; set; } = 0;
            /** transmit buffers */
            public byte[][] txbuf { get; set; } = new byte[EC_MAXBUF][];
            /** transmit buffer lengths */
            public int[] txbuflength { get; set; } = new int[EC_MAXBUF];
            /** temporary tx buffer */
            public byte[] txbuf2 { get; set; } = new byte[EC_MAXECATFRAME];
            /** temporary tx buffer length */
            public int txbuflength2 { get; set; } = 0;
            /** last used frame index */
            public byte lastidx { get; set; } = 0;
            /** current redundancy state */
            public int redstate { get; set; } = 0;
            /** pointer to redundancy port and buffers */
            public ecx_redportt redport { get; set; } = new ecx_redportt();
            public object getindex_mutex { get; set; } = new object();
            public object tx_mutex { get; set; } = new object();
            public object rx_mutex { get; set; } = new object();

            public ecx_portt()
            {
                for (int i = 0; i < EC_MAXBUF; i++)
                {
                    rxbuf[i] = new byte[EC_MAXECATFRAME];
                    txbuf[i] = new byte[EC_MAXECATFRAME];
                }
            }
        }
        public class ec_eepromt
        {
            public ushort comm { get; set; }
            public ushort addr { get; set; }
            public ushort d2 { get; set; }
        }
        public class ec_alstatust
        {
            public ushort alstatus { get; set; }
            public ushort unused { get; set; }
            public ushort alstatuscode { get; set; }
        }
        /** SII SM structure */
        public class ec_eepromSMt
        {
            public ushort Startpos { get; set; }
            public byte nSM { get; set; }
            public ushort PhStart { get; set; }
            public ushort Plength { get; set; }
            public byte Creg { get; set; }
            public byte Sreg { get; set; }       /* don't care */
            public byte Activate { get; set; }
            public byte PDIctrl { get; set; }      /* don't care */
        }
        ;
        /** SII FMMU structure */
        public class ec_eepromFMMUt
        {
            public ushort Startpos { get; set; }
            public byte nFMMU { get; set; }
            public byte FMMU0 { get; set; }
            public byte FMMU1 { get; set; }
            public byte FMMU2 { get; set; }
            public byte FMMU3 { get; set; }
        }
        public class ec_smt
        {
            public ushort StartAddr { get; set; }
            public ushort SMlength { get; set; }
            public uint SMflags { get; set; }

        }
        public class ec_fmmut
        {
            public uint LogStart { get; set; }
            public ushort LogLength { get; set; }
            public byte LogStartbit { get; set; }
            public byte LogEndbit { get; set; }
            public ushort PhysStart { get; set; }
            public byte PhysStartBit { get; set; }
            public byte FMMUtype { get; set; }
            public byte FMMUactive { get; set; }
            public byte unused1 { get; set; }
            public ushort unused2 { get; set; }
        }
        public class ec_slavet
        {
            /** state of slave */
            public ushort state { get; set; }
            /** AL status code */
            public ushort ALstatuscode { get; set; }
            /** Configured address */
            public ushort configadr { get; set; }
            /** Alias address */
            public ushort aliasadr { get; set; }
            /** Manufacturer from EEprom */
            public uint eep_man { get; set; }
            /** ID from EEprom */
            public uint eep_id { get; set; }
            /** revision from EEprom */
            public uint eep_rev { get; set; }
            /** serial number from EEprom */
            public uint eep_sn { get; set; }
            /** Interface type */
            public ushort Itype { get; set; }
            /** Device type */
            public ushort Dtype { get; set; }
            /** output bits */
            public ushort Obits { get; set; }
            /** output bytes, if Obits < 8 then Obytes = 0 */
            public uint Obytes { get; set; }
            /** output pointer in IOmap buffer */
            public byte[] outputs { get; set; } = new byte[4096];
            /** startbit in first output byte */
            public byte Ostartbit { get; set; }
            /** input bits */
            public ushort Ibits { get; set; }
            /** input bytes, if Ibits < 8 then Ibytes = 0 */
            public uint Ibytes { get; set; }
            /** input pointer in IOmap buffer */
            public byte[] inputs { get; set; } = new byte[4096];
            /** startbit in first input byte */
            public byte Istartbit { get; set; }
            /** SM structure */
            public ec_smt[] SM { get; set; } = new ec_smt[8];
            /** SM type 0=unused 1=MbxWr 2=MbxRd 3=Outputs 4=Inputs */
            public byte[] SMtype { get; set; } = new byte[8];
            /** FMMU structure */
            public ec_fmmut[] FMMU { get; set; } = new ec_fmmut[4] { new ec_fmmut(), new ec_fmmut(), new ec_fmmut(), new ec_fmmut() };
            /** FMMU0 function */
            public byte FMMU0func { get; set; }
            /** FMMU1 function */
            public byte FMMU1func { get; set; }
            /** FMMU2 function */
            public byte FMMU2func { get; set; }
            /** FMMU3 function */
            public byte FMMU3func { get; set; }
            /** length of write mailbox in bytes, if no mailbox then 0 */
            public ushort mbx_l { get; set; }
            /** mailbox write offset */
            public ushort mbx_wo { get; set; }
            /** length of read mailbox in bytes */
            public ushort mbx_rl { get; set; }
            /** mailbox read offset */
            public ushort mbx_ro { get; set; }
            /** mailbox supported protocols */
            public ushort mbx_proto { get; set; }
            /** Counter value of mailbox link layer protocol 1..7 */
            public byte mbx_cnt { get; set; }
            /** has DC capability */
            public bool hasdc { get; set; }
            /** Physical type; Ebus, EtherNet combinations */
            public byte ptype { get; set; }
            /** topology: 1 to 3 links */
            public byte topology { get; set; }
            /** active ports bitmap : ....3210 , set if respective port is active **/
            public byte activeports { get; set; }
            /** consumed ports bitmap : ....3210, used for internal delay measurement **/
            public byte consumedports { get; set; }
            /** slave number for parent, 0=master */
            public ushort parent { get; set; }
            /** port number on parent this slave is connected to **/
            public byte parentport { get; set; }
            /** port number on this slave the parent is connected to **/
            public byte entryport { get; set; }
            /** DC receivetimes on port A */
            public int DCrtA { get; set; }
            /** DC receivetimes on port B */
            public int DCrtB { get; set; }
            /** DC receivetimes on port C */
            public int DCrtC { get; set; }
            /** DC receivetimes on port D */
            public int DCrtD { get; set; }
            /** propagation delay */
            public int pdelay { get; set; }
            /** next DC slave */
            public ushort DCnext { get; set; }
            /** previous DC slave */
            public ushort DCprevious { get; set; }
            /** DC cycle time in ns */
            public int DCcycle { get; set; }
            /** DC shift from clock modulus boundary */
            public int DCshift { get; set; }
            /** DC sync activation, 0=off, 1=on */
            public byte DCactive { get; set; }
            /** link to config table */
            public ushort configindex { get; set; }
            /** link to SII config */
            public ushort SIIindex { get; set; }
            /** 1 = 8 bytes per read, 0 = 4 bytes per read */
            public byte eep_8byte { get; set; }
            /** 0 = eeprom to master , 1 = eeprom to PDI */
            public byte eep_pdi { get; set; }
            /** CoE details */
            public byte CoEdetails { get; set; }
            /** FoE details */
            public byte FoEdetails { get; set; }
            /** EoE details */
            public byte EoEdetails { get; set; }
            /** SoE details */
            public byte SoEdetails { get; set; }
            /** E-bus current */
            public short Ebuscurrent { get; set; }
            /** if >0 block use of LRW in processdata */
            public byte blockLRW { get; set; }
            /** group */
            public byte group { get; set; }
            /** first unused FMMU */
            public byte FMMUunused { get; set; }
            /** Boolean for tracking whether the slave is (not) responding, not used/set by the SOEM library */
            public bool islost { get; set; }
            ///** registered configuration function PO.SO, (DEPRECATED)*/
            //int (* PO2SOconfig) (uint16 slave);
            ///** registered configuration function PO.SO */
            //int (* PO2SOconfigx) (ecx_contextt* context, uint16 slave);
            /** mailbox handler state, 0 = no handler, 1 = cyclic task mbx handler, 2 = slave lost */

            /** readable name */
            public string name { get; set; } = string.Empty;

            public ec_slavet()
            {
            }
        };
        public class ec_groupt
        {
            /** logical start address for this group */
            public uint logstartaddr { get; set; }
            /** output bytes, if Obits < 8 then Obytes = 0 */
            public uint Obytes { get; set; }
            /** output pointer in IOmap buffer */
            public byte[] outputs { get; set; }
            /** input bytes, if Ibits < 8 then Ibytes = 0 */
            public uint Ibytes { get; set; }
            /** input pointer in IOmap buffer */
            public byte[] inputs { get; set; }
            /** has DC capability */
            public bool hasdc { get; set; }
            /** next DC slave */
            public ushort DCnext { get; set; }
            /** E-bus current */
            public short Ebuscurrent { get; set; }
            /** if >0 block use of LRW in processdata */
            public byte blockLRW { get; set; }
            /** IO segments used */
            public ushort nsegments { get; set; }
            /** 1st input segment */
            public ushort Isegment { get; set; }
            /** Offset in input segment */
            public ushort Ioffset { get; set; }
            /** Expected workcounter outputs */
            public ushort outputsWKC { get; set; }
            /** Expected workcounter inputs */
            public ushort inputsWKC { get; set; }
            /** check slave states */
            public bool docheckstate { get; set; }
            /** IO segmentation list. Datagrams must not break SM in two. */
            public uint[] IOsegment { get; set; } = new uint[EC_MAXIOSEGMENTS];
            /** pointer to out mailbox status register buffer */
            public byte[] mbxstatus { get; set; }
            /** mailbox status register buffer length */
            public int mbxstatuslength { get; set; }
            /** mailbox status lookup table */
            public ushort[] mbxstatuslookup { get; set; } = new ushort[EC_MAXSLAVE];
            /** mailbox last handled in mxbhandler */
            public ushort lastmbxpos { get; set; }
            /** mailbox  transmit queue struct */
            public ec_mbxqueuet mbxtxqueue { get; set; }

            public ec_groupt()
            {
                mbxtxqueue = new ec_mbxqueuet();
                outputs = new byte[4096];
                inputs = new byte[4096];
                mbxstatus = new byte[EC_MAXSLAVE];
            }
        }
        public class ec_mbxqueuet
        {
            public int listhead { get; set; }
            public int listtail { get; set; }
            public int listcount { get; set; }
            public byte[][] mbx { get; set; } = new byte[EC_MAXMBX + 1][];
            public int[] mbxstate { get; set; } = new int[EC_MBXPOOLSIZE];
            public int[] mbxremove { get; set; } = new int[EC_MBXPOOLSIZE];
            public int[] mbxticket { get; set; } = new int[EC_MBXPOOLSIZE];
            public ushort[] mbxslave { get; set; } = new ushort[EC_MBXPOOLSIZE];
            public object osal_mutext { get; set; } = new object();

            public ec_mbxqueuet()
            {
                for (int i = 0; i < mbx.Length; i++)
                {
                    mbx[i] = new byte[EC_MBXPOOLSIZE];
                }
            }
        }
        public class ec_eringt
        {
            public short head { get; set; }
            public short tail { get; set; }
            public ec_errort[] Error { get; set; }

            public ec_eringt()
            {
                Error = new ec_errort[EC_MAXELIST + 1];
            }
        }
        public class ec_idxstackT
        {
            public byte pushed { get; set; }
            public byte pulled { get; set; }
            public byte[] idx { get; set; } = new byte[EC_MAXBUF];
            public byte[] data { get; set; } = new byte[EC_MAXBUF];
            public ushort[] length { get; set; } = new ushort[EC_MAXBUF];
            public ushort[] dcoffset { get; set; } = new ushort[EC_MAXBUF];
            public byte[] type { get; set; } = new byte[EC_MAXBUF];

            public ec_idxstackT()
            {
            }
        }
        public class ec_SMcommtypet
        {
            public byte n { get; set; }
            public byte nu1 { get; set; }
            [FieldLengthAttribute(8)]
            public byte[] SMtype;
            public ec_SMcommtypet()
            {
                SMtype = new byte[EC_MAXSM];
            }
        }
        public class ec_PDOassignt
        {
            public byte n { get; set; }
            public byte nu1 { get; set; }
            [FieldLengthAttribute(256)]
            public short[] index;

            public ec_PDOassignt()
            {
                index = new short[256];
            }
        }
        public class ec_PDOdesct
        {
            public byte n { get; set; }
            public byte nu1 { get; set; }
            [FieldLengthAttribute(256)]
            public uint[] PDO;

            public ec_PDOdesct()
            {
                PDO = new uint[256];
            }
        }
        public class ec_mbxpoolt
        {
            public int listhead { get; set; }
            public int listtail { get; set; }
            public int listcount { get; set; }
            public int[] mbxemptylist { get; set; } = new int[EC_MBXPOOLSIZE];
            public object mbxmutex { get; set; } = new object();
            public byte[][] mbx { get; set; } = new byte[EC_MBXPOOLSIZE][];

            public ec_mbxpoolt()
            {
                for (int i = 0; i < mbx.Length; i++)
                {
                    mbx[i] = new byte[EC_MAXMBX + 1];
                }
            }
        }
        public class ec_enicoecmdt
        {
            /** transition(s) during which command should be sent */
            public ushort Transition { get; set; }
            /** complete access flag */
            public bool CA { get; set; }
            /** ccs (1 = read, 2 = write) */
            public byte Ccs { get; set; }
            /** object index */
            public ushort Index { get; set; }
            /** object subindex */
            public byte SubIdx { get; set; }
            /** timeout in us */
            public int Timeout { get; set; }
            /** size in bytes of parameter buffer */
            public int DataSize { get; set; }
            /** pointer to parameter buffer */
            public byte[] Data { get; set; } = new byte[4096];
        }
        public class ec_enislavet
        {
            public ushort Slave { get; set; }
            public uint VendorId { get; set; }
            public uint ProductCode { get; set; }
            public uint RevisionNo { get; set; }
            public ec_enicoecmdt CoECmds { get; set; } = new ec_enicoecmdt();
            public int CoECmdCount { get; set; }
        }
        public class ec_enit
        {
            public ec_enislavet slave { get; set; } = new ec_enislavet();
            public int slavecount { get; set; }
        }
        public class ecx_contextt
        {
            public ecx_portt port { get; set; }
            public ec_slavet[] slavelist { get; set; }
            public int slavecount { get; set; }
            public int maxslave { get; set; }
            public ec_groupt[] grouplist { get; set; }
            public int maxgroup { get; set; }
            public byte[] esibuf { get; set; }
            public uint[] esimap { get; set; }
            public ushort esislave { get; set; }
            public ec_eringt elist { get; set; }
            public ec_idxstackT idxstack { get; set; }
            public bool ecaterror { get; set; }
            public long DCtime { get; set; }

            /** internal, SM buffer */
            public ec_SMcommtypet[] SMcommtype { get; set; }
            /** internal, PDO assign list */
            public ec_PDOassignt[] PDOassign { get; set; }
            /** internal, PDO description list */
            public ec_PDOdesct[] PDOdesc { get; set; }
            /** internal, SM list from eeprom */
            public ec_eepromSMt eepSM { get; set; }
            /** internal, FMMU list from eeprom */
            public ec_eepromFMMUt eepFMMU { get; set; }
            /** internal, mailbox pool */
            public ec_mbxpoolt mbxpool { get; set; }

            /** registered FoE hook */
            //int (* FOEhook) (ushort slave, int packetnumber, int datasize);
            /** registered EoE hook */
            //int (* EOEhook) (ecx_contextt* context, ushort slave, void* eoembx);
            /** flag to control legacy automatic state change or manual state change */
            public int manualstatechange { get; set; }
            /** userdata, promotes application configuration esp. in EC_VER2 with multiple 
            * ec_context instances. Note: userdata memory is managed by application, not SOEM */
            public byte[] userdata { get; set; }

            public ecx_contextt()
            {
                port = new ecx_portt();
                esibuf = new byte[EC_MAXEEPBUF];
                esimap = new uint[EC_MAXEEPBITMAP];
                slavelist = Enumerable.Range(0, EC_MAXSLAVE).Select(_ => new ec_slavet()).ToArray();
                grouplist = Enumerable.Range(0, EC_MAXGROUP).Select(_ => new ec_groupt()).ToArray();
                SMcommtype = Enumerable.Range(0, EC_MAX_MAPT).Select(_ => new ec_SMcommtypet()).ToArray();
                PDOassign = Enumerable.Range(0, EC_MAX_MAPT).Select(_ => new ec_PDOassignt()).ToArray();
                PDOdesc = Enumerable.Range(0, EC_MAX_MAPT).Select(_ => new ec_PDOdesct()).ToArray();
                //EC_MAX_MAPT
                elist = new ec_eringt();
                idxstack = new ec_idxstackT();
                eepSM = new ec_eepromSMt();
                eepFMMU = new ec_eepromFMMUt();
                mbxpool = new ec_mbxpoolt();
                userdata = new byte[4096];
            }
        }

        public class ec_eepromPDOt
        {
            public ushort Startpos { get; set; }
            public ushort Length { get; set; }
            public ushort nPDO { get; set; }
            public ushort[] Index { get; set; } = new ushort[EC_MAXEEPDO];
            public ushort[] SyncM { get; set; } = new ushort[EC_MAXEEPDO];
            public ushort[] BitSize { get; set; } = new ushort[EC_MAXEEPDO];
            public ushort[] SMbitsize { get; set; } = new ushort[EC_MAXSM];
        }


        #endregion
        //public static byte[] StructToBytes<T>( T structData) where T : struct
        //{
        //    int size = Marshal.SizeOf<T>();
        //    byte[] bytes = new byte[size]; // 只分配一次
        //    MemoryMarshal.Write(bytes.AsSpan(), ref structData);
        //    return bytes;
        //}
        //public static byte[] StructToBytes<T>(T structData) where T : struct
        //{
        //    // 获取结构体的大小
        //    int size = Marshal.SizeOf<T>();
        //    // 创建字节数组
        //    byte[] bytes = new byte[size];
        //    // 分配非托管内存
        //    IntPtr ptr = Marshal.AllocHGlobal(size);

        //    try
        //    {
        //        // 将结构体写入非托管内存
        //        Marshal.StructureToPtr(structData, ptr, false);
        //        // 从非托管内存复制到字节数组
        //        Marshal.Copy(ptr, bytes, 0, size);
        //    }
        //    finally
        //    {
        //        // 释放非托管内存
        //        Marshal.FreeHGlobal(ptr);
        //    }
        //    return bytes;
        //}
        /// <summary>
        /// 将字节数组转换为指定结构体类型
        /// </summary>
        /// <typeparam name="T">目标结构体类型</typeparam>
        /// <param name="data">字节数组</param>
        /// <param name="offset">起始偏移量</param>
        /// <returns>转换后的结构体</returns>
        //public static T ToStructure<T>(Span<byte> data, int offset = 0) where T : struct
        //{
        //    // 确保字节数组有足够的数据
        //    int structSize = Marshal.SizeOf<T>();
        //    if (data.Length - offset < structSize)
        //        throw new ArgumentException($"Data array is too small. Expected at least {structSize} bytes, got {data.Length - offset}.");

        //    // 使用 MemoryMarshal 从 Span<byte> 读取结构体
        //    return MemoryMarshal.Read<T>(data.Slice(offset));
        //}
    }
}

