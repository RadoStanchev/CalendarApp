using CalendarApp.Services.Messages.Models;
using CalendarApp.Repositories.Messages;

namespace CalendarApp.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            this.messageRepository = messageRepository;
        }

        public async Task EnsureFriendshipAccessAsync(Guid userId, Guid friendshipId)
        {
            if (!await messageRepository.HasFriendshipAccessAsync(userId, friendshipId))
            {
                throw new InvalidOperationException("Приятелството не е намерено или достъпът е отказан.");
            }
        }

        public async Task EnsureMeetingAccessAsync(Guid userId, Guid meetingId)
        {
            if (!await messageRepository.HasMeetingAccessAsync(userId, meetingId))
            {
                throw new InvalidOperationException("Срещата не е намерена или достъпът е отказан.");
            }
        }

        public async Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Съдържанието на съобщението не може да бъде празно.", nameof(content));
            }

            await EnsureFriendshipAccessAsync(userId, friendshipId);
            var sender = await GetSenderAsync(userId);
            var messageId = await messageRepository.InsertAsync(userId, content, friendshipId: friendshipId);

            return new ChatMessageDto
            {
                FriendshipId = friendshipId,
                MessageId = messageId,
                SenderId = sender.Id,
                SenderName = sender.Name,
                Content = content,
                SentAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string?>()
            };
        }

        public async Task<ChatMessageDto> SaveMeetingMessageAsync(Guid userId, Guid meetingId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Съдържанието на съобщението не може да бъде празно.", nameof(content));
            }

            await EnsureMeetingAccessAsync(userId, meetingId);
            var sender = await GetSenderAsync(userId);
            var meeting = await messageRepository.GetMeetingInfoAsync(meetingId)
                ?? throw new InvalidOperationException("Срещата не беше намерена.");

            var messageId = await messageRepository.InsertAsync(userId, content, meetingId: meetingId);

            return new ChatMessageDto
            {
                MeetingId = meetingId,
                MessageId = messageId,
                SenderId = sender.Id,
                SenderName = sender.Name,
                Content = content,
                SentAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string?>
                {
                    ["meetingStartUtc"] = meeting.StartTime.ToUniversalTime().ToString("O"),
                    ["meetingLocation"] = meeting.Location
                }
            };
        }

        public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid userId, Guid friendshipId, int take)
        {
            await EnsureFriendshipAccessAsync(userId, friendshipId);
            return await messageRepository.GetRecentFriendshipMessagesAsync(friendshipId, take);
        }

        public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid userId, Guid meetingId, int take)
        {
            await EnsureMeetingAccessAsync(userId, meetingId);
            return await messageRepository.GetRecentMeetingMessagesAsync(meetingId, take);
        }

        private async Task<(Guid Id, string Name)> GetSenderAsync(Guid userId)
        {
            var (Id, FirstName, LastName) = await messageRepository.GetSenderAsync(userId)
                ?? throw new InvalidOperationException("Подателят не е намерен.");

            return (Id, string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))));
        }
    }
}
