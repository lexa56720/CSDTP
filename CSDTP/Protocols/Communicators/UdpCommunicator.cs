using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Communicators
{
    internal class UdpCommunicator : ICommunicator
    {
        public bool IsReceiving { get; private set; } = false;
        public bool IsAvailable { get; private set; } = true;

        public int ListenPort { get; private set; }


        private readonly UdpClient Client;
        private CancellationTokenSource? TokenSource;

        public event EventHandler<DataInfo>? DataAppear;
        public IPEndPoint? Destination { get; }
        private bool IsDisposed;
        private bool IsSending;
        public UdpCommunicator(IPEndPoint? destination = null)
        {
            ListenPort = PortUtils.GetFreePort(6660);
            Client = new UdpClient(ListenPort);
            Destination = destination;
        }
        public UdpCommunicator(int listenPort, IPEndPoint? destination = null)
        {

            Client = new UdpClient(listenPort);
            ListenPort = ((IPEndPoint)Client.Client.LocalEndPoint).Port;
            Destination = destination;
        }


        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            IsAvailable = false;
            if (!IsSending)
                Client.Dispose();
            Stop();
        }
        public async Task<bool> SendBytes(byte[] bytes)
        {
            if (Destination == null)
                return false;
            return await SendBytes(bytes, Destination);
        }
        public async Task<bool> SendBytes(byte[] bytes, IPEndPoint destionation)
        {
            if (!IsAvailable)
                return false;
            if (TokenSource == null || TokenSource.IsCancellationRequested)
                TokenSource = new CancellationTokenSource();
            try
            {
                var sended = await Client.SendAsync(bytes, destionation, TokenSource.Token);
                return sended == bytes.Length;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                if (IsDisposed)
                    Client.Dispose();
            }
        }

        public async ValueTask Start()
        {
            if (IsReceiving || IsDisposed)
                return;

            IsReceiving = true;

            if (TokenSource == null || TokenSource.IsCancellationRequested)
                TokenSource = new CancellationTokenSource();

            ReceiveWork(TokenSource.Token);
        }

        public async ValueTask Stop()
        {
            if (!IsReceiving)
                return;
            IsReceiving = false;
            TokenSource?.Cancel();
            TokenSource?.Dispose();
        }


        private async Task ReceiveWork(CancellationToken token)
        {
            while (IsReceiving)
            {
                try
                {
                    var data = await Client.ReceiveAsync(token);
                    OnDataAppear(data.Buffer, data.RemoteEndPoint);
                }
                catch (OperationCanceledException)
                {
                    if (IsDisposed)
                        break;
                }
            }
            if (IsDisposed)
                Client.Dispose();
        }


        private void OnDataAppear(byte[] buffer, IPEndPoint endPoint)
        {
            Func<byte[], Task<bool>> replyFunc = async (data) =>
            {
                if (data.Length > 0)
                    return await SendBytes(data, endPoint);
                return false;
            };
            DataAppear?.Invoke(this, new DataInfo(endPoint.Address, buffer, replyFunc));
        }
    }
}
