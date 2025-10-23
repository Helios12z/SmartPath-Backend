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
            CreateMap<Comment, CommentResponseDto>();
            CreateMap<Reaction, ReactionResponseDto>();
            CreateMap<Report, ReportResponseDto>();
            CreateMap<Friendship, FriendshipResponseDto>();
            CreateMap<Message, MessageResponseDto>();
            CreateMap<Notification, NotificationResponseDto>();
            CreateMap<SystemLog, SystemLogResponseDto>();
            CreateMap<Material, MaterialResponse>();
        }
    }
}
