using System.Collections.Concurrent;
using JiraClone.Application.DTOs.Chat;

namespace JiraClone.Application.Services
{
    public interface IConversationStateManager
    {
        ConversationState? GetConversation(ulong userId);
        void SaveConversation(ConversationState state);
        void ClearConversation(ulong userId);
        void CleanupOldConversations();
    }

    public class ConversationStateManager : IConversationStateManager
    {
        private readonly ConcurrentDictionary<ulong, ConversationState> _conversations = new();
        private readonly TimeSpan _conversationTimeout = TimeSpan.FromMinutes(30);

        public ConversationState? GetConversation(ulong userId)
        {
            if (_conversations.TryGetValue(userId, out var state))
            {
                // Check if conversation has expired
                if (DateTime.UtcNow - state.LastActivity > _conversationTimeout)
                {
                    _conversations.TryRemove(userId, out _);
                    return null;
                }

                state.LastActivity = DateTime.UtcNow;
                return state;
            }

            return null;
        }

        public void SaveConversation(ConversationState state)
        {
            state.LastActivity = DateTime.UtcNow;
            _conversations[state.UserId] = state;
        }

        public void ClearConversation(ulong userId)
        {
            _conversations.TryRemove(userId, out _);
        }

        public void CleanupOldConversations()
        {
            var now = DateTime.UtcNow;
            var expiredUsers = _conversations
                .Where(kvp => now - kvp.Value.LastActivity > _conversationTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var userId in expiredUsers)
            {
                _conversations.TryRemove(userId, out _);
            }
        }
    }
}
