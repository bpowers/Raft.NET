// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Threading.Tasks;

    internal sealed class Consensus
    {
        internal enum State
        {
            Disconnected = 1 << 0, // happens on construction and on config change
            Candidate    = 1 << 1,
            Leader       = 1 << 2,
            Follower     = 1 << 3,
        }

        private Config _config;
        private State _state = State.Disconnected;
        internal PeerRpcDelegate PerformPeerRpc { private get; set; }

        internal Consensus(Config config)
        {
            _config = config;
        }

        // Initialize this node, which means transitioning from
        // Disconnected -> Candidate -> (Leader || Follower)
        internal Task Init()
        {
            if (PerformPeerRpc == null)
                throw new InvalidOperationException("PerformPeerRpc must be set before Init");

            return Task.CompletedTask;
        }
    }
}
