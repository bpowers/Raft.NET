// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

    public struct PeerId
    {
        int N;
    }

    public class PeerRequest
    {
    }

    public class PeerResponse
    {
    }

    public class Server<TReadOp, TWriteOp, TValue>
    {
        public Server(Config config)
        {
        }

        IStateMachine<TReadOp, TWriteOp, TValue> _stateMachine;
        ILog<TWriteOp> _log;

        public Task<ClientResponse<TValue>> HandleClientRpcAsync(ClientRequest<TReadOp, TWriteOp> message)
        {
            var response = new ClientResponse<TValue>();

            return Task.FromResult(response);
        }

        public delegate Task<PeerResponse> PerformPeerRpc(PeerId peer, PeerRequest message);
    }
}
