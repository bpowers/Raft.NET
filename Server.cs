// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

    public struct PeerId
    {
        int N;

        public PeerId(int id)
        {
            N = id;
        }
    }

    public interface IPeerRequest {}

    public sealed class AppendEntriesRequest : IPeerRequest
    {
    }

    public sealed class RequestVoteRequest : IPeerRequest
    {
    }

    public interface IPeerResponse {}

    public sealed class AppendEntriesResponse : IPeerResponse
    {
    }

    public sealed class RequestVoteResponse : IPeerResponse
    {
    }

    public delegate Task<IPeerResponse> PeerRpcDelegate(PeerId peer, IPeerRequest request);

    public class Server<TReadOp, TWriteOp, TValue>
    {
        public Server(Config config)
        {
            _log = new Log<TWriteOp>(config);
            _consensus = new Consensus<TWriteOp>(config, _log);
        }

        private IStateMachine<TReadOp, TWriteOp, TValue> _stateMachine;
        private ILog<TWriteOp> _log;
        private Consensus<TWriteOp> _consensus;

        public Task<ClientResponse<TValue>> HandleClientRpcAsync(ClientRequest<TReadOp, TWriteOp> message)
        {
            var response = new ClientResponse<TValue>();

            return Task.FromResult(response);
        }

        // Initialize this node, which means transitioning from
        // Disconnected -> Candidate -> (Leader || Follower)
        public async Task Init()
        {
            await _consensus.Init();
        }
    }
}
