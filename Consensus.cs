// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class Consensus<TWriteOp>
    {
        internal enum State
        {
            Disconnected = 1 << 0, // happens on construction and on config change
            Candidate    = 1 << 1,
            Leader       = 1 << 2,
            Follower     = 1 << 3,
        }

        // TODO: Persistent state; should be persisted to disk
        private Term _currentTerm = new Term(0);
        private PeerId? _votedFor = null;
        private ILog<TWriteOp> _log;

        // Volatile state
        private LogIndex _commitIndex = new LogIndex(0);
        private LogIndex _lastApplied = new LogIndex(0);

        // Leader state
        private Dictionary<PeerId, int> _nextIndex;
        private Dictionary<PeerId, int> _matchIndex;

        private Config _config;
        private State _state = State.Disconnected;
        internal PeerRpcDelegate _peerRpc;

        internal PeerId Id { get; private set; }

        private Random _random;
        private CancellationTokenSource _followerCancellationSource;
        private DateTime _lastHeartbeat;

        internal Consensus(PeerId id, Config config, ILog<TWriteOp> log)
        {
            _config = config;
            Id = id;
            _random = new Random(_config.PrngSeed[id]);
            _peerRpc = _config.PeerRpcDelegate;
            if (_peerRpc == null)
                throw new InvalidOperationException("PerformPeerRpc must be set in Config");
            _log = log;
        }

        private Task TransitionToCandidate()
        {
            Console.WriteLine("transitioning to a candidate");
            throw new NotImplementedException();
        }

        private TimeSpan RandomElectionTimeout()
        {
            var timeoutSpan = (int)_config.ElectionTimeoutSpan.TotalMilliseconds;
            var randomWait = Time.Milliseconds(_random.Next(timeoutSpan));
            return _config.ElectionTimeoutMin.Add(randomWait);
        }

        private async Task FollowerTask(CancellationToken token)
        {
            var timeout = RandomElectionTimeout();

            // loop, sleeping for ~ the broadcast (timeout) time.  If
            // we have gone too long without
            while (!_followerCancellationSource.IsCancellationRequested)
            {
                var sinceHeartbeat = DateTime.Now - _lastHeartbeat;
                if (sinceHeartbeat > timeout)
                {
                    await TransitionToCandidate();
                    return;
                }

                try
                {
                    await Task.Delay(timeout - sinceHeartbeat, token);
                }
                catch (TaskCanceledException)
                {
                    // cancelled or disposed means we are no longer a
                    // follower, so end this task
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        private Task TransitionToFollower()
        {
            // we can transition to a follower from any state _but_ Follower
            Debug.Assert(_state != State.Follower);

            _followerCancellationSource = new CancellationTokenSource();

            // TODO: wrap this in a try catch to report errors
            Task.Run(() => FollowerTask(_followerCancellationSource.Token));

            return Task.CompletedTask;
        }

        // Initialize this node, which means transitioning from
        // Disconnected -> Candidate -> (Leader || Follower)
        internal async Task Init()
        {
            Debug.Assert(_state == State.Disconnected);

            _lastHeartbeat = DateTime.Now;

            await TransitionToFollower();
        }
    }
}
