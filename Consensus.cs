// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class VoteLedger
    {
        readonly int PeerCount;
        readonly int QuorumSize;

        public IList<PeerId> Votes { get; set; } = new List<PeerId>();
        // explicit responses we've received where we didn't get the
        // vote. |Votes| + |Nacks| == TotalReplyCount
        public IList<PeerId> Nacks { get; set; } = new List<PeerId>();

        private bool _completed;
        private CancellationToken _token;
        private TaskCompletionSource<bool> _completionSource;

        internal VoteLedger(Config config, CancellationToken token)
        {
            _token = token;
            _completionSource = new TaskCompletionSource<bool>();

            _token.Register(() => {
                    _completed = true;
                    _completionSource.TrySetCanceled();
                });

            // TODO: if we have e.g. 6 peers -- is this actually
            // correct?  I think so, for majority we need > 50% (half
            // is not majority)
            QuorumSize = config.Peers.Count / 2 + 1;
            PeerCount = config.Peers.Count;
        }

        internal void Record(RequestVoteResponse response)
        {
            if (_completed)
                return;

            // TODO: what to do with response.Term?

            if (response.VoteGranted)
            {
                Votes.Add(response.Sender);
            }
            else
            {
                Nacks.Add(response.Sender);
            }


            if (Votes.Count >= QuorumSize || Nacks.Count >= QuorumSize ||
                Votes.Count + Nacks.Count == PeerCount)
            {
                _completed = true;
                // we use TrySetResult here, in case we either race
                // with other Record() calls, or a cancel.
                _completionSource.TrySetResult(Votes.Count >= QuorumSize);
            }
        }

        internal Task<bool> WaitMajority(IEnumerable<Task<IPeerResponse>> responses, CancellationToken cancellationToken)
        {
            foreach (var responseTask in responses)
            {
                Task.Run(async () => {
                        try
                        {
                            var result = await responseTask;
                            if (result is RequestVoteResponse response)
                            {
                                this.Record(response);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"WaitMajority exception: {e}");
                        }
                    }, _token);
            }

            return _completionSource.Task;
        }

        bool Elected()
        {
            return Votes.Count >= QuorumSize;
        }

        bool VoteFailed()
        {
            return Nacks.Count >= QuorumSize;
        }
    }

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

        internal async Task<IPeerResponse> HandleVoteRequest(RequestVoteRequest request)
        {
            bool voteGranted = false;
            var lastTerm = Term.Invalid;
            // FIXME: should _lastApplied be -1, and check for that here?
            if (_log.Length > 0)
                lastTerm = _log.Get(_lastApplied).Term;

            // if (((_state == State.Candidate && request.Term > _currentTerm) || (request.Term >= _currentTerm)) &&
            if (request.Term >= _currentTerm &&
                (!_votedFor.HasValue || _votedFor.Value == request.CandidateId) &&
                (request.LastLogIndex >= _lastApplied && request.LastLogTerm >= lastTerm))
            {
                _currentTerm = request.Term;

                if (_state != State.Follower) {
                    await TransitionToFollower();
                }

                voteGranted = true;
                _votedFor = request.CandidateId;
                ResetElectionTimer();
            }

            return new RequestVoteResponse()
            {
                Sender = Id,
                Term = _currentTerm,
                VoteGranted = voteGranted,
            };
        }

        internal Task<IPeerResponse> HandlePeerRpc(IPeerRequest request)
        {
            switch (request) {
                case RequestVoteRequest voteRequest:
                    return HandleVoteRequest(voteRequest);
                default:
                    Console.WriteLine($"{Id.N} Got UNHANDLED PeerRpc request {request}");
                    break;
            }

            return Task.FromResult((IPeerResponse)null);
        }

        private Task TransitionToLeader()
        {
            // only a candidate can be a leader
            Debug.Assert(_state == State.Candidate, $"Expected state to be Candidate, not {_state}");

            // cancel any previous election we are a candidate in (e.g. it timed out)
            if (_electionCancellationSource != null)
            {
                _electionCancellationSource.Cancel();
                _electionCancellationSource.Dispose();
                _electionCancellationSource = null;
            }

            Console.WriteLine($"Look at me, I ({Id.N}) am the leader now of term {_currentTerm.N}.");

            // send now, and TODO: repeat during idle periods to prevent election timeout
            Heartbeat();

            return Task.CompletedTask;
        }

        private void HandleClientRequest()
        {
            // If command received from client: append entry to local log,
            // respond after entry applied to state machine (§5.3)

            // If last log index ≥ nextIndex for a follower: send
            // AppendEntries RPC with log entries starting at nextIndex

            // If successful: update nextIndex and matchIndex for
            // follower (§5.3)

            // If AppendEntries fails because of log inconsistency:
            // decrement nextIndex and retry (§5.3)

            // If there exists an N such that N > commitIndex, a majority
            // of matchIndex[i] ≥ N, and log[N].term == currentTerm:
            // set commitIndex = N (§5.3, §5.4).
        }

        private void Heartbeat()
        {
            // send AppendEntries RPCs to each server
            // if that server's nextIndex <= log index, send w/ log entries starting at nextIndex
            // else send empty AppendEntries RPC
        }

        private void SendAppendEntriesRpc()
        {

        }

        private async Task TransitionToCandidate()
        {
            // Leaders and disconnected servers cannot transition
            // directly to candidates.
            Debug.Assert(_state == State.Follower || _state == State.Candidate);

            // ensure we are now in Candidate state
            _state = State.Candidate;

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

            var ledger = new VoteLedger(_config, cancellationToken);
            // vote for ourselves
            ledger.Record(new RequestVoteResponse()
                {
                    Sender = Id,
                    Term = _currentTerm,
                    VoteGranted = true,
                });

            // record that this round, we are voting for ourselves
            _votedFor = Id;

            // we reset the election timer right before
            // TransitionToCandidate is called

            var lastTerm = Term.Invalid;
            // FIXME: should _lastApplied be -1, and check for that here?
            if (_log.Length > 0)
                lastTerm = _log.Get(_lastApplied).Term;

            IEnumerable<Task<IPeerResponse>> responses =
                _config.Peers.Where(id => id.N != Id.N).Select(id => _peerRpc(id, new RequestVoteRequest()
                    {
                        Term = _currentTerm,
                        CandidateId = Id,
                        LastLogIndex = _lastApplied,
                        LastLogTerm = lastTerm,
                    }));

            bool receivedMajority = false;
            try
            {
                receivedMajority = await ledger.WaitMajority(responses, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (!receivedMajority)
            {
                _electionCancellationSource.Cancel();
                _electionCancellationSource.Dispose();
                _electionCancellationSource = null;
                return;
            }

            Console.WriteLine($"Node({Id.N}): received {ledger.Votes.Count}/{_config.Peers.Count} ({ledger.Votes.Count + ledger.Nacks.Count} responses)");

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
            var timeout = RandomElectionTimeout();

            // loop, sleeping for ~ the broadcast (timeout) time.  If
            // we have gone too long without
            while (!_timeoutCancellationSource.IsCancellationRequested)
            {
                var sinceHeartbeat = DateTime.Now - _lastHeartbeat;
                if (sinceHeartbeat > timeout)
                {
                    ResetElectionTimer();
                    Supervised.Run(function: TransitionToCandidate);
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

        private Task TransitionToFollower()
        {
            // we can transition to a follower from any state _but_ Follower
            Debug.Assert(_state != State.Follower);

            _state = State.Follower;
            _timeoutCancellationSource = new CancellationTokenSource();

            Supervised.Run(() => ElectionTimeoutTask(_timeoutCancellationSource.Token));

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
