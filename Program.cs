// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static IDictionary<PeerId, KeyValueStore<int>> peers = new Dictionary<PeerId, KeyValueStore<int>>();
        static Task<IPeerResponse> HandlePeerRpc(PeerId peer, IPeerRequest request)
        {
            return peers[peer].Server.HandlePeerRpc(request);
        }

        static int RandomInt32(RandomNumberGenerator rng)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        static IDictionary<PeerId, int> GenerateRandomSeeds(List<PeerId> peerIds)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                return peerIds.ToDictionary(p => p, p => RandomInt32(rng));
            }
        }

        static async Task Main(string[] args)
        {
            var peerIds = new List<PeerId>()
            {
                new PeerId(1),
                new PeerId(2),
                new PeerId(3),
            };

            var config = new Config()
            {
                Peers = peerIds,
                PrngSeed = GenerateRandomSeeds(peerIds),
                PeerRpcDelegate = HandlePeerRpc,
            };

            peers = peerIds.ToDictionary(id => id, id => new KeyValueStore<int>(id, config));

            await Task.WhenAll(peers.Values.Select(async (peer) => await peer.Init()).ToArray());

            Console.WriteLine("All peers initialized.");

            await Task.Delay(Timeout.Infinite);
        }
    }
}
