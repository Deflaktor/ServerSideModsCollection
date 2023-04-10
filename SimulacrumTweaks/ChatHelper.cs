using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SimulacrumTweaks
{
    public static class ChatHelper
    {
        private const string RedColor = "ff0000";
        private const string GreenColor = "32cd32";
        private const string SilverColor = "c0c0c0";

        public static void PlayerHasTooManyItems(string userName)
        {
            var message = $"<color=#{RedColor}>{userName} is being greedy!</color>";
            RoR2.Chat.SendBroadcastChat(new RoR2.Chat.SimpleChatMessage { baseToken = message });
        }
    }
}
