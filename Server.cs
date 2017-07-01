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

    public class PeerRequest
    {
    }

    public class PeerResponse
    {
    }

    public delegate Task<PeerResponse> PeerRpcDelegate(PeerId peer, PeerRequest request);

    public class Server<TReadOp, TWriteOp, TValue>
    {
        public Server(Config config)
        {
            _consensus = new Consensus(config);
        }

        private IStateMachine<TReadOp, TWriteOp, TValue> _stateMachine;
        private ILog<TWriteOp> _log;
        private PeerRpcDelegate _performPeerRpc;
        private Consensus _consensus;

        public Task<ClientResponse<TValue>> HandleClientRpcAsync(ClientRequest<TReadOp, TWriteOp> message)
        {
            var response = new ClientResponse<TValue>();

            return Task.FromResult(response);
        }

        public PeerRpcDelegate PerformPeerRpc
        {
            set
            {
                _performPeerRpc = value;
                _consensus.PerformPeerRpc = value;
            }
        }

        // Initialize this node, which means transitioning from
        // Disconnected -> Candidate -> (Leader || Follower)
        public async Task Init()
        {
            await _consensus.Init();
        }
    }
}
