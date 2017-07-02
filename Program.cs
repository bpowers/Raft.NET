// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    class Program
    {
        static Task<IPeerResponse> HandlePeerRpc(PeerId peer, IPeerRequest request)
        {
            Console.WriteLine("Got PeerRpc request");

            return Task.FromResult((IPeerResponse)null);
        }

        static int RandomInt32(RandomNumberGenerator rng)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        static async Task Main(string[] args)
        {
            var peers = new List<PeerId>()
            {
                new PeerId(1),
                new PeerId(2),
                new PeerId(3),
            };

            var rng = RandomNumberGenerator.Create();
            var seeds = peers.ToDictionary(p => p, p => RandomInt32(rng));
            rng.Dispose();
            rng = null;

            var config = new Config()
            {
                Peers = peers,
                PrngSeed = seeds,
                PeerRpcDelegate = HandlePeerRpc,
            };

            var keyValueStore0 = new KeyValueStore<int>(config.Peers[0], config);

            await keyValueStore0.Init();

            Console.WriteLine("Hello World!");
        }
    }
}
