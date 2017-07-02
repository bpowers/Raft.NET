// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public struct PeerId
    {
        int N;

        public PeerId(int id)
        {
            N = id;
        }
    }

    public interface IPeerRequest
    {
        // TODO: add Serialize method
    }

    internal struct AppendEntriesRequest<T> : IPeerRequest
    {
        Term                Term         { get; set; }
        PeerId              LeaderId     { get; set; }
        LogIndex            PrevLogIndex { get; set; }
        Term                PrevLogTerm  { get; set; }
        IList<ILogEntry<T>> Entries      { get; set; }
        LogIndex            LeaderCommit { get; set; }
    }

    internal struct RequestVoteRequest : IPeerRequest
    {
        Term     Term         { get; set; }
        PeerId   CandidateId  { get; set; }
        LogIndex LastLogIndex { get; set; }
        Term     LastLogTerm  { get; set; }
    }

    public interface IPeerResponse
    {
        // TODO: add Serialize method
    }

    internal struct AppendEntriesResponse : IPeerResponse
    {
        Term Term    { get; set; } // for leader to update itself
        bool Success { get; set; } // true if follower contained entry matching PrevLogIndex and PrevLogTerm
    }

    internal struct RequestVoteResponse : IPeerResponse
    {
        Term Term        { get; set; } // for leader to update itself
        bool VoteGranted { get; set; } // true if candidate received vote
    }

    // Delegate definition for embedder to expose to Raft Server.
    public delegate Task<IPeerResponse> PeerRpcDelegate(PeerId peer, IPeerRequest request);
}
