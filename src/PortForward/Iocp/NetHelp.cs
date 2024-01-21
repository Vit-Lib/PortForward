﻿using Sers.CL.Socket.Iocp;
using System.Net;
using System.Net.Sockets;

namespace Vit.Core.Util.Net
{
    public class NetHelp
    {
        #region ParseToIPAddress
        public static IPAddress ParseToIPAddress(string host)
        {
            IPAddress ipAddress;
            #region 获取ip地址
            if (!IPAddress.TryParse(host, out ipAddress))
            {
                IPHostEntry hostInfo = Dns.GetHostEntry(host);
                ipAddress = hostInfo.AddressList[0];
            }
            #endregion
            return ipAddress;
        }
        #endregion


        
    }
}
