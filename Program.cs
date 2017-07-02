// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class Program
    {
        static Task<IPeerResponse> HandlePeerRpc(PeerId peer, IPeerRequest request)
        {
            Console.WriteLine("Got PeerRpc request");

            return Task.FromResult((IPeerResponse)null);
        }

        static async Task Main(string[] args)
        {
            var config = new Config()
            {
                Peers = new List<PeerId>()
                {
                    new PeerId(1),
                    new PeerId(2),
                    new PeerId(3),
                },
                PeerRpcDelegate = HandlePeerRpc,
            };

            var keyValueStore0 = new KeyValueStore<int>(config, config.Peers[0]);

            await keyValueStore0.Init();

            Console.WriteLine("Hello World!");
        }
    }
}
