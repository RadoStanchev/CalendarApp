using System;
using System.Globalization;
using AutoMapper;
using CalendarApp.Models.Chat;
using CalendarApp.Services.Friendships.Models;
using CalendarApp.Services.Meetings.Models;
using CalendarApp.Services.Messages.Models;

namespace CalendarApp.Infrastructure.Mapping
{
    public class ChatProfile : Profile
    {
        public ChatProfile()
        {
            CreateMap<ChatMessageDto, ChatMessageViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.MessageId))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.SenderName ?? string.Empty));

            CreateMap<FriendshipThreadDto, ChatThreadViewModel>()
                .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.FriendshipId))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(_ => ThreadType.Friendship))
                .ForMember(dest => dest.FriendshipId, opt => opt.MapFrom(src => src.FriendshipId))
                .ForMember(dest => dest.FriendId, opt => opt.MapFrom(src => src.FriendId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.FriendFirstName} {src.FriendLastName}"))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => $"{src.FriendFirstName[0]}{src.FriendLastName[0]}"))
                .ForMember(dest => dest.AccentClass, opt => opt.MapFrom(src => ChatViewModelHelper.GetAccentClass(src.FriendId)))
                .ForMember(dest => dest.LastMessagePreview, opt => opt.MapFrom(src => src.LastMessageContent ?? string.Empty))
                .ForMember(dest => dest.LastMessageAt, opt => opt.MapFrom(src => src.LastMessageSentAt))
                .ForMember(dest => dest.LastActivityLabel, opt => opt.MapFrom(src => ChatViewModelHelper.BuildActivityLabel(src.LastMessageSentAt)))
                .ForMember(dest => dest.IsOnline, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Meeting, opt => opt.Ignore());

            CreateMap<MeetingThreadDto, ChatThreadViewModel>()
                .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.MeetingId))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(_ => ThreadType.Meeting))
                .ForMember(dest => dest.FriendshipId, opt => opt.Ignore())
                .ForMember(dest => dest.FriendId, opt => opt.Ignore())
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => ChatViewModelHelper.BuildMeetingTitle(src.StartTime, src.Location)))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(_ => string.Empty))
                .ForMember(dest => dest.AccentClass, opt => opt.MapFrom(src => ChatViewModelHelper.GetAccentClass(src.MeetingId)))
                .ForMember(dest => dest.LastMessagePreview, opt => opt.MapFrom(src => src.LastMessageContent ?? string.Empty))
                .ForMember(dest => dest.LastMessageAt, opt => opt.MapFrom(src => src.LastMessageSentAt))
                .ForMember(dest => dest.LastActivityLabel, opt => opt.MapFrom(src => ChatViewModelHelper.BuildActivityLabel(src.LastMessageSentAt)))
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.Meeting, opt => opt.MapFrom((src, _, __, context) =>
                {
                    var title = ChatViewModelHelper.BuildMeetingTitle(src.StartTime, src.Location);
                    var userId = context.Items.TryGetValue("CurrentUserId", out var value) && value is Guid id ? id : Guid.Empty;

                    return new MeetingThreadMetadata
                    {
                        MeetingId = src.MeetingId,
                        Title = title,
                        StartTimeUtc = DateTime.SpecifyKind(src.StartTime, DateTimeKind.Utc),
                        Location = src.Location,
                        IsOrganizer = src.CreatedById == userId,
                        ParticipantCount = src.ParticipantCount
                    };
                }));
        }
    }
}

