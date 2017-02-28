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
        private ConcurrentQueue<EventPublication> _localPublishQueue = new ConcurrentQueue<EventPublication>();
        private NetMQPoller _poller = new NetMQPoller();
        private PushSocket _nodeSocket;
        private List<PullSocket> _peerSockets = new List<PullSocket>();
        private List<string> _peerConnectionStrings;
        private string _localConnectionString;
        private bool _active = false;
        
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

        public async Task<string> DequeueForProcessing()
        {
            string id;
            if (_localRunQueue.TryDequeue(out id))
            {
                return id;
            }
            return null;
        }

        public async Task<EventPublication> DequeueForPublishing()
        {            
            EventPublication item;
            if (_localPublishQueue.TryDequeue(out item))
            {
                return item;
            }
            return null;
        }
        
        public async Task QueueForProcessing(string Id)
        {
            PushMessage(Message.FromWorkflowId(Id));
        }

        public async Task QueueForPublishing(EventPublication item)
        {
            PushMessage(Message.FromPublication(item));
        }

        public void Start()
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
                case MessageType.Publication:
                    _localPublishQueue.Enqueue(msg.ToEventPublication());
                    break;
            }
        }

        public void Stop()
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
        
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
