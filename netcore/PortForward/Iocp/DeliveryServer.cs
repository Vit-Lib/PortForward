﻿//  https://freshflower.iteye.com/blog/2285272 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Vit.Core.Module.Log;
using Vit.Core.Util.Net;
using Vit.Core.Util.Pool;

namespace Sers.CL.Socket.Iocp
{
    public class DeliveryServer 
    { 

        /// <summary>
        /// 服务端 监听地址。若不指定则监听所有网卡。例如： "127.0.0.1"、"sersms.com"。
        /// </summary>
        public string host = null;        
        
        /// <summary>
        /// 服务端 监听端口号(默认4501)
        /// </summary>
        public int port = 4501;
        
        /// <summary>
        /// 缓存区大小
        /// </summary>
        public int receiveBufferSize = 8 * 1024;

   
        public Action<DeliveryConnection> Conn_OnConnected { private get; set; }      
 
 
    
        public bool Start()
        {
            Stop();

            try
            {
                //Logger.Info("[CL.DeliveryServer] Socket.Iocp,starting... host:" + host + " port:" + port);

                connMap.Clear();

                IPEndPoint localEndPoint = new IPEndPoint(String.IsNullOrEmpty(host)?IPAddress.Any: NetHelp.ParseToIPAddress(host), port);
                listenSocket = new global::System.Net.Sockets.Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndPoint);

                 
                // start the server with a listen backlog of 100 connections
                listenSocket.Listen(maxConnectCount);
                // post accepts on the listening socket
                StartAccept(null);

                //Logger.Info("[CL.DeliveryServer] Socket.Iocp,started.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
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

            //(x.1) stop conn
            ConnectedList.ToList().ForEach(conn=>conn.Close());        
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
                Logger.Error(ex);
            }
        }
       
 


  


        public DeliveryServer()
        {
            MaxConnCount = 20000;
        }


        /// <summary>
        /// 最大连接数
        /// </summary>
        private int maxConnectCount;


        public int MaxConnCount { 
            get { return maxConnectCount; }
            set 
            {
                maxConnectCount = value;
                m_maxNumberAcceptedClients = new Semaphore(maxConnectCount, maxConnectCount);
                //pool_ReceiveEventArgs.Capacity = maxConnectCount;
            }
        }


        global::System.Net.Sockets.Socket listenSocket;
 
        Semaphore m_maxNumberAcceptedClients;

        /// <summary>
        ///  connHashCode -> DeliveryConnection
        /// </summary>
        readonly ConcurrentDictionary<int, DeliveryConnection> connMap = new ConcurrentDictionary<int, DeliveryConnection>();

        public IEnumerable<DeliveryConnection> ConnectedList => connMap.Values.Select(conn=>((DeliveryConnection)conn));





        #region ReceiveEventArgs

        SocketAsyncEventArgs ReceiveEventArgs_Create(global::System.Net.Sockets.Socket socket)
        {
            var conn = Delivery_OnConnected(socket);           

            SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs();
            receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);


            //var buff = DataPool.BytesGet(receiveBufferSize);
            var buff = new byte[receiveBufferSize];
            receiveEventArgs.SetBuffer(buff, 0, buff.Length);
 
            receiveEventArgs.UserToken = conn;
            conn.receiveEventArgs = receiveEventArgs;

            return receiveEventArgs;
        }


        void ReceiveEventArgs_Release(SocketAsyncEventArgs receiveEventArgs)
        {
            receiveEventArgs.UserToken = null;
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

                if (!acceptEventArgs.AcceptSocket.ReceiveAsync(receiveEventArgs))
                {
                    ProcessReceive(receiveEventArgs);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
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
                    Logger.Info("[Iocp]IO_Completed Send");
                    return;
                //    ProcessSend(e);
                //    break;
                default:
                    Logger.Info("[Iocp]IO_Completed default");
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
                DeliveryConnection conn = (DeliveryConnection)e.UserToken;
                if (conn != null)
                {
                    // check if the remote host closed the connection               
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        //读取数据
                        conn.AppendData(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred));

                        //byte[] buffData = DataPool.BytesGet(receiveBufferSize);
                        byte[] buffData = new byte[receiveBufferSize];
                        e.SetBuffer(buffData, 0, buffData.Length);

                        // start loop
                        //继续接收. 为什么要这么写,请看Socket.ReceiveAsync方法的说明
                        if (!conn.socket.ReceiveAsync(e))
                            ProcessReceive(e);
                    }
                    else
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        #region Delivery_Event

        private DeliveryConnection Delivery_OnConnected(global::System.Net.Sockets.Socket socket)
        {
            var conn = new DeliveryConnection();
 
            conn.Init(socket);           

            conn.Conn_OnDisconnected = Delivery_OnDisconnected;

            connMap[conn.GetHashCode()] = conn;
            try
            {
                Conn_OnConnected?.Invoke(conn);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return conn;
        }

        void Delivery_OnDisconnected(DeliveryConnection conn)
        {
            // decrement the counter keeping track of the total number of clients connected to the server
            m_maxNumberAcceptedClients.Release();    

            ReceiveEventArgs_Release(conn.receiveEventArgs);

            connMap.TryRemove(conn.GetHashCode(),out _);

            //conn.Close();
        }
        #endregion


    }
}
