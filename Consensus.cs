// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
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

        internal Consensus(Config config, ILog<TWriteOp> log)
        {
            _config = config;
            _peerRpc = _config.PeerRpcDelegate;
            if (_peerRpc == null)
                throw new InvalidOperationException("PerformPeerRpc must be set in Config");
            _log = log;
        }

        // Initialize this node, which means transitioning from
        // Disconnected -> Candidate -> (Leader || Follower)
        internal Task Init()
        {
            return Task.CompletedTask;
        }
    }
}
