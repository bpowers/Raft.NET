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
        static Task<PeerResponse> PeerRpc(PeerId peer, PeerRequest request)
        {
            Console.WriteLine("Got PeerRpc request");

            var response = new PeerResponse();
            return Task.FromResult(response);
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
            };

            var KVStore0 = new KVStore<int>(config, config.Peers[0])
            {
                PerformPeerRpc = PeerRpc,
            };

            await KVStore0.Init();

            Console.WriteLine("Hello World!");
        }
    }
}
