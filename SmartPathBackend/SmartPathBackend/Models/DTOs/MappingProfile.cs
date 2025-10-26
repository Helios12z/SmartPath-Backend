using AutoMapper;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Models.DTOs
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserResponseDto>();
            CreateMap<Post, PostResponseDto>();
            CreateMap<Comment, CommentResponseDto>()
                .ForMember(d=>d.AuthorUsername, o=>o.MapFrom(s=>s.Author.Username))
                .ForMember(d=>d.AuthorAvatarUrl, o=>o.MapFrom(s=>s.Author.AvatarUrl))
                .ForMember(d=>d.AuthorPoint, o=>o.MapFrom(s=>s.Author.Point));
            CreateMap<Reaction, ReactionResponseDto>();
            CreateMap<Report, ReportResponseDto>();
            CreateMap<Friendship, FriendshipResponseDto>();

            CreateMap<Message, MessageResponseDto>()
            .ForMember(d => d.ChatId, opt => opt.MapFrom(s => s.ChatId))
            .ForMember(d => d.SenderUsername, opt => opt.MapFrom(s => s.Sender.Username));
            CreateMap<Chat, ChatResponseDto>()
                .ForMember(d => d.Member1Id, opt => opt.MapFrom(s => s.Member1Id))
                .ForMember(d => d.Member2Id, opt => opt.MapFrom(s => s.Member2Id))
                .ForMember(d => d.Messages,
                    opt => opt.MapFrom(s => (s.Messages ?? new List<Message>()).OrderBy(m => m.CreatedAt)));

            CreateMap<Notification, NotificationResponseDto>();
            CreateMap<SystemLog, SystemLogResponseDto>();
            CreateMap<Material, MaterialResponse>();

            CreateMap<BotConversation, BotConversationResponse>()
            .ForMember(d => d.MessageCount, o => o.MapFrom(s => (s.Messages ?? new List<BotMessage>()).Count));
            CreateMap<BotMessage, BotMessageResponse>();
        }
    }
}
