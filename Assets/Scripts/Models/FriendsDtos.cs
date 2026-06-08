using System;
using System.Collections.Generic;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class PlayerSearchResultDto
    {
        public Guid PlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AvatarId { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class FriendEntryDto
    {
        public Guid PlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AvatarId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int MyWins { get; set; }
        public int MyLosses { get; set; }
    }

    [Serializable]
    public sealed class FriendsListResponseDto
    {
        public List<FriendEntryDto> Friends { get; set; } = new();
        public int Count { get; set; }
        public int Max { get; set; }
    }

    [Serializable]
    public sealed class FriendRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid FromPlayerId { get; set; }
        public string FromUsername { get; set; } = string.Empty;
        public string FromAvatarId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    [Serializable]
    public sealed class SendFriendRequestBodyDto
    {
        public Guid TargetPlayerId { get; set; }
    }

    [Serializable]
    public sealed class FriendChallengeReceivedDto
    {
        public Guid ChallengeId { get; set; }
        public Guid FromPlayerId { get; set; }
        public string FromUsername { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class FriendChallengeDeclinedDto
    {
        public Guid ChallengeId { get; set; }
    }

    [Serializable]
    public sealed class FriendChallengeExpiredDto
    {
        public Guid ChallengeId { get; set; }
    }
}
