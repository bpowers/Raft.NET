// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

    public sealed class KVStore<T>
    {
        public struct GetRequest
        {
            public string Key { get; set; }
        }

        public struct PutRequest<TValue>
        {
            string Key { get; set; }
            TValue Value { get; set; }
        }

        private PeerId _id;
        private Server<GetRequest, PutRequest<T>, T> _server;

        public KVStore(Config config, PeerId self)
        {
            _id = self;
            _server = new Server<GetRequest, PutRequest<T>, T>(config);
        }

        public PeerRpcDelegate PerformPeerRpc
        {
            set
            {
                _server.PerformPeerRpc = value;
            }
        }

        public Task Init()
        {
            return _server.Init();
        }
    }
}
