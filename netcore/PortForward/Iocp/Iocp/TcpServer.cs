//  https://freshflower.iteye.com/blog/2285272 


using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static PortForward.Iocp.Iocp.TcpConn;

namespace PortForward.Iocp.Iocp
{
    public class TcpServer
    {
        /// <summary>
        /// Mq 服务端 监听地址。若不指定则监听所有网卡。例如： "127.0.0.1"、"sersms.com"。
        /// </summary>
        public string host = null;
        /// <summary>
        /// Mq 服务端 监听端口号。例如： 10345。
        /// </summary>
        public int port = 10345;



        /// <summary>
        /// 缓存区大小
        /// </summary>
        public int receiveBufferSize = 8 * 1024;

        /// <summary>
        /// 请勿处理耗时操作，需立即返回
        /// </summary>
        public Action<TcpConn> Conn_OnDisconnected { get; set; }

        /// <summary>
        /// 请勿处理耗时操作，需立即返回
        /// </summary>
        public Action<TcpConn> Conn_OnConnected { get; set; }


        /// <summary>
        /// 请勿处理耗时操作，需立即返回。收到数据事件
        /// </summary>
        public Action<TcpConn, ArraySegment<byte>> Conn_OnGetData { set; get; }
 
 
    
        public bool Start()
        {
            Stop();

            try
            {
                TcpConn.LogMessage("[ServerMq] Socket.Iocp,starting... host:" + host + " port:" + port);

                connMap.Clear();

                IPEndPoint localEndPoint = new IPEndPoint(String.IsNullOrEmpty(host)?IPAddress.Any: TcpConn.ParseToIPAddress(host), port);
                listenSocket = new global::System.Net.Sockets.Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndPoint);

                 
                // start the server with a listen backlog of 100 connections
                listenSocket.Listen(maxConnectCount);
                // post accepts on the listening socket
                StartAccept(null);

                TcpConn.LogMessage("[ServerMq] Socket.Iocp,started.");
                return true;
            }
            catch (Exception ex)
            {
                TcpConn.LogError(ex);
            }
            return false;
        }


        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (listenSocket == null) return;

            var listenSocket_ = listenSocket;
            listenSocket = null;

            //(x.1) stop mqConn
            ConnectedList.ToList().ForEach(conn => {conn.OnDisconnected(conn); });
            connMap.Clear(); 

            //(x.2) close Socket
            try
            {
                listenSocket_.Close();
                listenSocket_.Dispose();
                //listenSocket_.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                TcpConn.LogError(ex);
            }
            
        }
       
 


  


        public TcpServer()
        {
            MaxConnCount = 20000;
        }


        /// <summary>
        /// 最大连接数
        /// </summary>
        private int maxConnectCount;


        public int MaxConnCount { get { return maxConnectCount; }
            set {
                maxConnectCount = value;
                m_maxNumberAcceptedClients = new Semaphore(maxConnectCount, maxConnectCount);
                pool_ReceiveEventArgs.Capacity = maxConnectCount;
            }
        }


        global::System.Net.Sockets.Socket listenSocket;
 
        Semaphore m_maxNumberAcceptedClients;

        /// <summary>
        ///  connGuid -> MqConnect
        /// </summary>
        public readonly ConcurrentDictionary<int, TcpConn> connMap = new ConcurrentDictionary<int, TcpConn>();

        public IEnumerable<TcpConn> ConnectedList => connMap.Values;





        #region ReceiveEventArgs


        SocketAsyncEventArgs ReceiveEventArgs_Create(global::System.Net.Sockets.Socket socket)
        {
            var conn = new TcpConn();
            conn.Init(socket);
            conn.OnGetData = Conn_OnGetData;

            conn.OnDisconnected = MqConn_Release;
            conn.OnDisconnected += Conn_OnDisconnected;

            var receiveEventArgs = pool_ReceiveEventArgs.PopOrNull();
            if (receiveEventArgs == null)
            {
                receiveEventArgs = new SocketAsyncEventArgs();
                receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            }

            var buff =  new byte[receiveBufferSize];
            receiveEventArgs.SetBuffer(buff, 0, buff.Length);
 
            receiveEventArgs.UserToken = conn;
            conn.receiveEventArgs = receiveEventArgs;

            return receiveEventArgs;
        }

        ObjectPool<SocketAsyncEventArgs> pool_ReceiveEventArgs = new ObjectPool<SocketAsyncEventArgs>();

        void ReceiveEventArgs_Release(SocketAsyncEventArgs receiveEventArgs)
        {
            receiveEventArgs.UserToken = null;
            pool_ReceiveEventArgs.Push(receiveEventArgs);
        }
        #endregion



        // Begins an operation to accept a connection request from the client 
        //
        // <param name="acceptEventArg">The context object to use when issuing 
        // the accept operation on the server's listening socket</param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArgs.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            if (!listenSocket.AcceptAsync(acceptEventArgs))
            {
                AcceptEventArg_Completed(null,acceptEventArgs);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync 
        // operations and is invoked when an accept operation is complete
        //
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {          
                // Get the socket for the accepted client connection and put it into the 
                //ReadEventArg object user token
                SocketAsyncEventArgs receiveEventArgs = ReceiveEventArgs_Create(acceptEventArgs.AcceptSocket);

                TcpConn mqConn = (TcpConn)receiveEventArgs.UserToken;

                //if (mqConn != null)
                {
                    connMap[mqConn.GetHashCode()] = mqConn;

                    try
                    {
                        Conn_OnConnected?.Invoke(mqConn);
                    }
                    catch (Exception ex)
                    {
                        TcpConn.LogError(ex);
                    }
                }

                if (!acceptEventArgs.AcceptSocket.ReceiveAsync(receiveEventArgs))
                {
                    ProcessReceive(receiveEventArgs);
                }
            }
            catch (Exception ex)
            {
                TcpConn.LogError(ex);
            }

            // Accept the next connection request
            if (acceptEventArgs.SocketError == SocketError.OperationAborted) return;
            StartAccept(acceptEventArgs);
        }


        public void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    //Logger.Info("[Iocp]IO_Completed Send");
                    return;
                //    ProcessSend(e);
                //    break;
                default:
                    //Logger.Info("[Iocp]IO_Completed default");
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }


        // This method is invoked when an asynchronous receive operation completes. 
        // If the remote host closed the connection, then the socket is closed.  
        // If data was received then the data is echoed back to the client.
        //
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                //读取数据
                TcpConn mqConn = (TcpConn)e.UserToken;
                if (mqConn != null)
                {
                    // check if the remote host closed the connection               
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        //读取数据
                        mqConn.OnGetData(mqConn,new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred));

                        byte[] buffData = new byte[receiveBufferSize];
                        e.SetBuffer(buffData, 0, buffData.Length);

                        // start loop
                        //继续接收. 为什么要这么写,请看Socket.ReceiveAsync方法的说明
                        if (!mqConn.socket.ReceiveAsync(e))
                            ProcessReceive(e);
                    }
                    else
                    {
                        mqConn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TcpConn.LogError(ex);
            }
        }

       

  
        private void MqConn_Release(TcpConn conn)
        {
            // decrement the counter keeping track of the total number of clients connected to the server
            m_maxNumberAcceptedClients.Release();
 
            ReceiveEventArgs_Release(conn.receiveEventArgs);

            connMap.TryRemove(conn.GetHashCode(),out _);            

        }



    }
}
