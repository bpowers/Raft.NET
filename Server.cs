// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

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
