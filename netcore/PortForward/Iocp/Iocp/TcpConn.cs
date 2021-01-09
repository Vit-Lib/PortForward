using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;


namespace PortForward.Iocp.Iocp
{
    public class TcpConn 
    {
        #region ObjectPool
        public class ObjectPool<T>
       where T : new()
        {

            public static ObjectPool<T> Shared = new ObjectPool<T>();


            private ConcurrentBag<T> _objects = new ConcurrentBag<T>();

            /// <summary>
            /// Gets or sets the total number of elements the internal data structure can hold without resizing.(default:100000)
            /// </summary>
            public int Capacity = 100000;


            public T Pop()
            {
                //return new T();
                return _objects.TryTake(out var item) ? item : new T();
            }

            public T PopOrNull()
            {
                return _objects.TryTake(out var item) ? item : default(T);
            }

            public void Push(T item)
            {
                if (_objects.Count > Capacity) return;
                _objects.Add(item);
            }
        }
        #endregion

        #region static
        public static Action<Exception> LogError = (ex) => { };
        public static Action<string> LogMessage = (msg) => { };


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

        #endregion
       

        internal SocketAsyncEventArgs receiveEventArgs;
 

        /// <summary>
        /// 连接状态(0:waitForCertify; 2:certified; 4:waitForClose; 8:closed;)
        /// </summary>
        //public byte state { get; set; }

     



        /// <summary>
        /// 请勿处理耗时操作，需立即返回。接收到客户端的数据事件   
        /// </summary>
        public Action<TcpConn, ArraySegment<byte>> OnGetData;

        public Action<TcpConn> OnDisconnected { get; set; }



     
     

        public void SendDataAsync(List<ArraySegment<byte>> data)
        {
            if (data == null) return;
            socket.SendAsync(data, SocketFlags.None);
        }
        public void SendDataAsync(ArraySegment<byte> data)
        {
            //if (data == null) return;
            socket.SendAsync(data, SocketFlags.None);
        }


        public void Close()
        {
            if (socket == null) return; 

            var socket_ = socket;
            socket = null;          

            try
            {
                socket_.Close();
                socket_.Dispose();

                //socket_.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            try
            {
                OnDisconnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

        }
        public void Init(global::System.Net.Sockets.Socket socket)
        {
            this.socket = socket;
            connectTime = DateTime.Now;  
        }
 
        /// <summary>
        /// 通信SOCKET
        /// </summary>
        public global::System.Net.Sockets.Socket socket { get;private set; }

        /// <summary>
        /// 连接时间
        /// </summary>
        private DateTime connectTime { get; set; }


         








    }
}
