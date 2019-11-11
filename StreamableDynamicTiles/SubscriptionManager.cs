using LibDeltaSystem.Db.System.Entities;
using StreamableDynamicTiles.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace StreamableDynamicTiles
{
    public static class SubscriptionManager
    {
        /// <summary>
        /// Holder of subs
        /// </summary>
        private static List<TileSubscription> subscriptions = new List<TileSubscription>();

        /// <summary>
        /// Subscribes a client to a tile
        /// </summary>
        public static void SubscribeTile(DTSession session, DynamicTileTarget target, int token)
        {
            lock(subscriptions)
            {
                subscriptions.Add(new TileSubscription
                {
                    session = session,
                    target = target,
                    token = token
                });
            }
        }

        /// <summary>
        /// Unsubscribes a client from a tile
        /// </summary>
        /// <param name="session"></param>
        /// <param name="target"></param>
        public static void UnsubscribeTile(DTSession session, int token)
        {
            lock(subscriptions)
                subscriptions.RemoveAll(x => x.token == token && x.session == session);
        }

        /// <summary>
        /// Sends events to all subscribed clients
        /// </summary>
        /// <param name="target"></param>
        /// <param name="url"></param>
        public static void SendEvent(DynamicTileTarget target, string url)
        {
            //Find targets
            List<TileSubscription> targets;
            lock (subscriptions)
                targets = subscriptions.Where(x => x.target.Compare(target)).ToList();

            //Send targets
            foreach (var t in targets)
                t.session.SendTileLoadMessage(t.target.map_id, t.token, url);
        }
    }
}
