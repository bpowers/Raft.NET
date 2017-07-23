// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

    public sealed class KeyValueStore<T>
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

        internal class StateMachine: IStateMachine<GetRequest, PutRequest<T>, T>
        {
            public Task<IStateMachine<GetRequest, PutRequest<T>, T>> ApplyAsync(PutRequest<T> operation)
            {
                IStateMachine<GetRequest, PutRequest<T>, T> stateMachine = this;

                return Task.FromResult(stateMachine);
            }

            public Task<T> ReadAsync(GetRequest operation)
            {
                return Task.FromResult(default(T));
            }
        }

        private PeerId _id;
        private Server<GetRequest, PutRequest<T>, T> _server;

        public Server<GetRequest, PutRequest<T>, T> Server
        {
            get { return _server; }
        }

        public KeyValueStore(PeerId id, Config config)
        {
            _id = id;
            _server = new Server<GetRequest, PutRequest<T>, T>(id, config);
        }

        public Task Init()
        {
            return _server.Init();
        }
    }
}
