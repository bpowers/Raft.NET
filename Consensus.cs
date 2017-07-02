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
        private CancellationTokenSource _timeoutCancellationSource;
        private DateTime _lastHeartbeat;

        private CancellationTokenSource _electionCancellationSource;

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

        private Task TransitionToLeader()
        {
            throw new NotImplementedException();
        }

        private async Task<bool> WaitMajority(IList<Task<RequestVoteResponse>> responses, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task TransitionToCandidate()
        {
            // Leaders and disconnected servers cannot transition
            // directly to candidates.
            Debug.Assert(_state == State.Follower || _state == State.Candidate);

            // cancel any previous election we are a candidate in (e.g. it timed out)
            if (_electionCancellationSource != null)
            {
                _electionCancellationSource.Cancel();
                _electionCancellationSource.Dispose();
                _electionCancellationSource = null;
            }

            _electionCancellationSource = new CancellationTokenSource();
            var cancellationToken = _electionCancellationSource.Token;

            _currentTerm.N++;

            // need data structure for vote responses (both positive + neg)

            // vote for self

            // we reset the election timer right before
            // TransitionToCandidate is called

            // send voterpc messages to other servers
            var requests = new List<Task<RequestVoteResponse>>();

            var receivedMajority = await WaitMajority(requests, cancellationToken);
            if (!receivedMajority)
            {
                _electionCancellationSource.Cancel();
                _electionCancellationSource.Dispose();
                _electionCancellationSource = null;
                return;
            }

            await TransitionToLeader();
        }

        // TODO: this isn't an _election_ timeout, it is a timeout
        // that results in an election
        private TimeSpan RandomElectionTimeout()
        {
            var timeoutSpan = (int)_config.ElectionTimeoutSpan.TotalMilliseconds;
            var randomWait = Time.Milliseconds(_random.Next(timeoutSpan));
            return _config.ElectionTimeoutMin.Add(randomWait);
        }

        private async Task ElectionTimeoutTask(CancellationToken cancelationToken)
        {
            try
            {
                var timeout = RandomElectionTimeout();

                // loop, sleeping for ~ the broadcast (timeout) time.  If
                // we have gone too long without
                while (!_timeoutCancellationSource.IsCancellationRequested)
                {
                    var sinceHeartbeat = DateTime.Now - _lastHeartbeat;
                    if (sinceHeartbeat > timeout)
                    {
                        ResetElectionTimer();
                        Task.Run(function: TransitionToCandidate);
                        continue;
                    }

                    try
                    {
                        await Task.Delay(timeout - sinceHeartbeat, cancelationToken);
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
            catch (Exception exception)
            {
                Console.WriteLine("Error in election timeout task: " + exception);
            }
        }

        private Task TransitionToFollower()
        {
            // we can transition to a follower from any state _but_ Follower
            Debug.Assert(_state != State.Follower);

            _state = State.Follower;
            _timeoutCancellationSource = new CancellationTokenSource();

            // TODO: wrap this in a try catch to report errors
            Task.Run(() => ElectionTimeoutTask(_timeoutCancellationSource.Token));

            return Task.CompletedTask;
        }

        private void ResetElectionTimer()
        {
            _lastHeartbeat = DateTime.Now;
        }

        // Initialize this node, which means transitioning from
        // Disconnected -> Candidate -> (Leader || Follower)
        internal async Task Init()
        {
            Debug.Assert(_state == State.Disconnected);

            ResetElectionTimer();

            await TransitionToFollower();
        }
    }
}
