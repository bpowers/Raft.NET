// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public struct PeerId
    {
        internal int N;

        public PeerId(int n)
        {
            N = n;
        }
    }

    public interface IPeerRequest
    {
        // TODO: add Serialize method
    }

    internal struct AppendEntriesRequest<T> : IPeerRequest
    {
        internal Term                Term         { get; set; }
        internal PeerId              LeaderId     { get; set; }
        internal LogIndex            PrevLogIndex { get; set; }
        internal Term                PrevLogTerm  { get; set; }
        internal IList<ILogEntry<T>> Entries      { get; set; }
        internal LogIndex            LeaderCommit { get; set; }
    }

    internal struct RequestVoteRequest : IPeerRequest
    {
        internal Term     Term         { get; set; }
        internal PeerId   CandidateId  { get; set; }
        internal LogIndex LastLogIndex { get; set; }
        internal Term     LastLogTerm  { get; set; }
    }

    public interface IPeerResponse
    {
        // TODO: add Serialize method
    }

    internal struct AppendEntriesResponse : IPeerResponse
    {
        internal PeerId Sender  { get; set; }
        internal Term   Term    { get; set; } // for leader to update itself
        internal bool   Success { get; set; } // true if follower contained entry matching PrevLogIndex and PrevLogTerm
    }

    internal struct RequestVoteResponse : IPeerResponse
    {
        internal PeerId Sender      { get; set; }
        internal Term   Term        { get; set; } // for leader to update itself
        internal bool   VoteGranted { get; set; } // true if candidate received vote
    }

    // Delegate definition for embedder to expose to Raft Server.
    public delegate Task<IPeerResponse> PeerRpcDelegate(PeerId peer, IPeerRequest request);
}
