using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.ZeroMQ.Models;

namespace WorkflowCore.LockProviders.ZeroMQ.Services
{
    public class ZeroMQLockProvider : IDistributedLockProvider
    {        
        private List<DistributedLock> _lockRegistry = new List<DistributedLock>();
        private List<PendingLock> _pendingLocks = new List<PendingLock>();
        private List<PendingLock> _pendingReleases = new List<PendingLock>();

        private ConcurrentDictionary<Guid, DateTime> _peerLastContact = new ConcurrentDictionary<Guid, DateTime>();
        private Guid _nodeId = Guid.NewGuid();
        private List<string> _peerConnectionStrings;
        private string _localConnectionString;
        private List<DealerSocket> _peerClients = new List<DealerSocket>();
        private RouterSocket _server = new RouterSocket(); //todo: dependency injection
        private NetMQPoller _poller = new NetMQPoller(); //todo: dependency injection
        private TimeSpan _lockTTL = TimeSpan.FromMinutes(5);
        private ILogger _logger;
        private NetMQTimer _houseKeeper;

        public ZeroMQLockProvider(int port, IEnumerable<string> peers, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ZeroMQLockProvider>();
            _localConnectionString = "tcp://*:" + Convert.ToString(port);
            _peerConnectionStrings = new List<string>();
            foreach (var peer in peers)
                _peerConnectionStrings.Add("tcp://" + peer);

            _server.ReceiveReady += Server_ReceiveReady;    
        }
        
        public async Task<bool> AcquireLock(string Id)
        {            
            if (_lockRegistry.Any(x => x.ResourceId == Id))
                return false;

            PendingLock pendingLock = new PendingLock();
            pendingLock.ResourceId = Id;
            _pendingLocks.Add(pendingLock);

            var peerList = _peerLastContact.Where(x => x.Value >= (DateTime.Now.AddMinutes(-2))).Select(x => x.Key).ToList();
            int requestCount = peerList.Count();
            int peerQuorum = (requestCount / 2) + 1;
            if (requestCount == 0)
                peerQuorum = 0;

            foreach (var peerId in peerList)
            {
                _server
                    .SendMoreFrame(peerId.ToByteArray())
                    .SendMoreFrame(_nodeId.ToByteArray())
                    .SendMoreFrame(ConvertOp(MessageOp.Acquire))
                    .SendFrame(Id);                
            }

            Task<bool> task = new Task<bool>(() =>
            {
                DateTime expiry = DateTime.Now.AddSeconds(10);
                _logger.LogDebug("({0}) Waiting for quorum of {1} on {2}, expires at {3}", _nodeId, peerQuorum, Id, expiry);
                while ((pendingLock.Responses.Count() < peerQuorum) && (!pendingLock.Responses.Any(x => !x.Value)) && (DateTime.Now < expiry))
                {
                    System.Threading.Thread.Sleep(10);
                }
                _logger.LogDebug("({0}) Remote responses on {1}, count {2} of {3}", _nodeId, Id, pendingLock.Responses.Count(), peerQuorum);
                var result = (pendingLock.Responses.Count(x => x.Value) >= peerQuorum) && (pendingLock.Responses.Count(x => !x.Value) == 0);
                if (!result)
                {
                    foreach (var rollbackPeer in pendingLock.Responses.Where(x => x.Value).Select(x => x.Key).ToList())
                    {
                        _server
                            .SendMoreFrame(rollbackPeer.ToByteArray())
                            .SendMoreFrame(_nodeId.ToByteArray())
                            .SendMoreFrame(ConvertOp(MessageOp.Release))
                            .SendFrame(Id);
                    }
                }
                else
                {
                    DistributedLock distLock = new DistributedLock();
                    distLock.NodeId = _nodeId;
                    distLock.ResourceId = Id;
                    distLock.Expiry = DateTime.Now.Add(_lockTTL);
                    lock (_lockRegistry)
                        _lockRegistry.Add(distLock);
                }
                lock (_pendingLocks)
                    _pendingLocks.Remove(pendingLock);
                return result;
            });
            task.Start();
            return await task;
        }

        public async Task ReleaseLock(string Id)
        {
            lock (_lockRegistry)
            {
                var localLocks = _lockRegistry.Where(x => x.ResourceId == Id && x.NodeId == _nodeId).ToList();
                foreach (var local in localLocks)
                    _lockRegistry.Remove(local);
            }

            var peerList = _peerLastContact.Select(x => x.Key).ToList();
            var activePeerCount = _peerLastContact.Where(x => x.Value >= (DateTime.Now.AddMinutes(-2))).Count();            
            int peerQuorum = (activePeerCount / 2) + 1;
            if (activePeerCount == 0)
                peerQuorum = 0;

            PendingLock pendingRelease = new PendingLock();
            pendingRelease.ResourceId = Id;
            _pendingReleases.Add(pendingRelease);

            foreach (var peerId in peerList)
            {
                _server
                    .SendMoreFrame(peerId.ToByteArray())
                    .SendMoreFrame(_nodeId.ToByteArray())
                    .SendMoreFrame(ConvertOp(MessageOp.Release))
                    .SendFrame(Id);                
            }

            Task task = new Task(() =>
            {
                DateTime expiry = DateTime.Now.AddSeconds(10);                
                while (pendingRelease.Responses.Count() < peerQuorum) 
                {
                    System.Threading.Thread.Sleep(10);
                }
                _pendingReleases.Remove(pendingRelease);
            });
            task.Start();
            await task;
        }

        public void Start()
        {
            _server.Bind(_localConnectionString);
            _poller.Add(_server);
            _poller.RunAsync();

            foreach (var connStr in _peerConnectionStrings)
            {
                DealerSocket peer = new DealerSocket();
                peer.Options.Identity = _nodeId.ToByteArray();
                peer.ReceiveReady += Peer_ReceiveReady;
                peer.Connect(connStr);
                _poller.Add(peer);
                peer.SendFrame(ConvertOp(MessageOp.Ping));
            }

            _houseKeeper = new NetMQTimer(TimeSpan.FromSeconds(60));
            _houseKeeper.Elapsed += HouseKeeper_Elapsed;
            _poller.Add(_houseKeeper);
            _houseKeeper.Enable = true;

        }
                
        public void Stop()
        {
            var peerList = _peerLastContact.Select(x => x.Key).ToList();
            foreach (var peerId in peerList)
            {
                _server
                    .SendMoreFrame(peerId.ToByteArray())
                    .SendMoreFrame(_nodeId.ToByteArray())
                    .SendMoreFrame(ConvertOp(MessageOp.Disconnect));                
            }

            _poller.Stop();
            _poller.Remove(_server);
            _poller.Remove(_houseKeeper);
            _houseKeeper.Enable = false;
            _server.Close();
        }

        private void Peer_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();
            if (message.FrameCount > 1)
            {
                var serverId = new Guid(message[0].Buffer);
                var op = (MessageOp)(message[1].Buffer.First());                                
                _peerLastContact[serverId] = DateTime.Now;

                switch (op)
                {
                    case MessageOp.Ping:
                        e.Socket                            
                            .SendMoreFrame(ConvertOp(MessageOp.Pong))
                            .SendFrame(_nodeId.ToByteArray());
                        break;                    
                    case MessageOp.Acquire:
                        string acqureLockId = message[2].ConvertToString();
                        _logger.LogDebug("({0}) Recv acquire on {1} from {2}", _nodeId, acqureLockId, serverId);
                        lock (_lockRegistry)
                        {
                            bool existingLock = _pendingLocks.Any(x => x.ResourceId == acqureLockId);
                            if (!existingLock)
                                existingLock = _lockRegistry.Any(x => x.ResourceId == acqureLockId);

                            if (!existingLock)
                            {
                                _logger.LogDebug("({0}) Remote acquire on {1} from {2} success", _nodeId, acqureLockId, serverId);
                                var distLock = new DistributedLock();
                                distLock.Expiry = DateTime.Now.Add(_lockTTL);
                                distLock.NodeId = serverId;
                                distLock.ResourceId = acqureLockId;
                                lock (_lockRegistry)
                                    _lockRegistry.Add(distLock);
                                e.Socket
                                    .SendMoreFrame(ConvertOp(MessageOp.LockReserved))
                                    .SendFrame(acqureLockId);
                            }
                            else
                            {
                                _logger.LogDebug("({0}) Remote acquire on {1} from {2} fail", _nodeId, acqureLockId, serverId);
                                e.Socket
                                    .SendMoreFrame(ConvertOp(MessageOp.LockFailed))
                                    .SendFrame(acqureLockId);
                            }
                        }
                        break;
                    case MessageOp.Release:
                        string releaseLockId = message[2].ConvertToString();
                        lock (_lockRegistry)
                        {
                            var existingLock1 = _lockRegistry.FirstOrDefault(x => x.ResourceId == releaseLockId && x.NodeId == serverId);
                            if (existingLock1 != null)
                                _lockRegistry.Remove(existingLock1);
                            e.Socket
                                .SendMoreFrame(ConvertOp(MessageOp.LockReleased))
                                .SendFrame(releaseLockId);
                        }
                        break;
                    case MessageOp.Disconnect:
                        if (_peerLastContact.ContainsKey(serverId))
                        {
                            DateTime lastContact;
                            _peerLastContact.TryRemove(serverId, out lastContact);
                        }
                        break;
                }
            }
        }

        private void Server_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();
            if (message.FrameCount > 1)
            {
                var clientId = new Guid(message[0].Buffer);
                var op = (MessageOp)(message[1].Buffer.First());
                _peerLastContact[clientId] = DateTime.Now;

                switch (op)
                {
                    case MessageOp.Ping:
                        e.Socket
                            .SendMoreFrame(message[0].Buffer)
                            .SendMoreFrame(_nodeId.ToByteArray())
                            .SendMoreFrame(ConvertOp(MessageOp.Pong))
                            .SendFrame(_nodeId.ToByteArray());
                        break;
                    case MessageOp.LockReserved:
                        var reservedId = message[2].ConvertToString();
                        var pendingReserved = _pendingLocks.Where(x => x.ResourceId == reservedId).ToList();
                        foreach (var pending in pendingReserved)
                            pending.Responses[clientId] = true;
                        break;
                    case MessageOp.LockFailed:
                        var failedId = message[2].ConvertToString();
                        var pendingFailed = _pendingLocks.Where(x => x.ResourceId == failedId).ToList();
                        foreach (var pending in pendingFailed)
                            pending.Responses[clientId] = false;
                        break;
                    case MessageOp.LockReleased:
                        var releaseId = message[2].ConvertToString();
                        var pendingRelease = _pendingReleases.Where(x => x.ResourceId == releaseId).ToList();
                        foreach (var pending in pendingRelease)
                            pending.Responses[clientId] = true;
                        break;                    
                }                                
            }
        }

        private void HouseKeeper_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            _logger.LogDebug("Performing house keeping");
            lock (_lockRegistry)
            {
                var expiredList = _lockRegistry.Where(x => x.Expiry < DateTime.Now).ToList();
                foreach (var expired in expiredList)
                {
                    _lockRegistry.Remove(expired);
                }
            }
        }


        private byte[] ConvertOp(MessageOp op)
        {
            byte[] result = new byte[1];
            result[0] = (byte)op;
            return result;
        }

        enum MessageOp { Disconnect = 0, Acquire = 1, Release = 2, LockReserved = 3, LockFailed = 4, LockReleased = 5, Ping = 6, Pong = 7 }
    }
}
