using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.QueueProviders.ZeroMQ.Models;

namespace WorkflowCore.QueueProviders.ZeroMQ.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class ZeroMQProvider : IQueueProvider
    {
        private ILogger _logger;
        private ConcurrentQueue<string> _localRunQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _localPublishQueue = new ConcurrentQueue<string>();
        private NetMQPoller _poller = new NetMQPoller();
        private PushSocket _nodeSocket;
        private List<PullSocket> _peerSockets = new List<PullSocket>();
        private List<string> _peerConnectionStrings;
        private string _localConnectionString;
        private bool _active = false;

        public bool IsDequeueBlocking => false;
        public ZeroMQProvider(int port, IEnumerable<string> peers, bool canTakeWork, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ZeroMQProvider>();
            _localConnectionString = "@tcp://*:" + Convert.ToString(port);
            _peerConnectionStrings = new List<string>();

            if (canTakeWork)
            {
                _peerConnectionStrings.Add(">tcp://localhost:" + Convert.ToString(port));
                foreach (var peer in peers)
                    _peerConnectionStrings.Add(">tcp://" + peer);
            }
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    PushMessage(Message.FromWorkflowId(id));
                    break;
                case QueueType.Event:
                    PushMessage(Message.FromEventId(id));
                    break;
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (SelectQueue(queue).TryDequeue(out string id))
                return id;

            return null;
        }        

        public async Task Start()
        {
            _nodeSocket = new PushSocket(_localConnectionString);
            _poller.Add(_nodeSocket);
            _poller.RunAsync();
            _active = true;
            foreach (var connStr in _peerConnectionStrings)
            {
                PullSocket peer = new PullSocket(connStr);
                peer.ReceiveReady += Peer_ReceiveReady;
                _poller.Add(peer);
                _peerSockets.Add(peer);                
            }            
        }

        private void Peer_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {            
            string data = e.Socket.ReceiveFrameString();
            _logger.LogDebug("{0} - Got remote item {1}", _localConnectionString, data);
            var msg = JsonConvert.DeserializeObject<Message>(data);
            switch (msg.MessageType)
            {
                case MessageType.Workflow:
                    _localRunQueue.Enqueue(msg.Content);
                    break;
                case MessageType.Event:
                    _localPublishQueue.Enqueue(msg.Content);
                    break;
            }
        }

        public async Task Stop()
        {
            _active = false;            
            _poller.Stop();
            
            _poller.Remove(_nodeSocket);
            foreach (var peer in _peerSockets)
            {
                _poller.Remove(peer);
                peer.Close();
            }
            _peerSockets.Clear();
            _nodeSocket.Close();            
        }

        private void PushMessage(Message message)
        {
            if (!_active)
                throw new Exception("ZeroMQ provider not started");

            var str = JsonConvert.SerializeObject(message);
            if (!_nodeSocket.TrySendFrame(TimeSpan.FromSeconds(3), str))
                throw new Exception("Unable to send message");
        }
        
        public void Dispose()
        {
            if (_active)
                Stop();
        }

        private ConcurrentQueue<string> SelectQueue(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return _localRunQueue;
                case QueueType.Event:
                    return _localPublishQueue;
            }
            return null;
        }

    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
