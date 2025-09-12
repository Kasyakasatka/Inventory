using AutoMapper;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Inventory, InventoryViewDTO>()
            .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.UserName))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));
        CreateMap<InventoryDTO, Inventory>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.TagsInput != null
                ? src.TagsInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
                : new List<string>()));
        CreateMap<Inventory, InventoryDTO>()
            .ForMember(dest => dest.TagsInput, opt => opt.MapFrom(src => src.Tags != null ? string.Join(", ", src.Tags) : string.Empty));

        CreateMap<FieldDefinitionDTO, FieldDefinition>()
            .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
            .ForMember(dest => dest.ShowInTable, opt => opt.MapFrom(src => src.ShowInTable));

        CreateMap<FieldDefinition, FieldDefinitionDTO>()
            .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
            .ForMember(dest => dest.ShowInTable, opt => opt.MapFrom(src => src.ShowInTable));

        CreateMap<IdElement, IdElement>().ReverseMap();

        CreateMap<ItemDTO, Item>()
            .ForMember(dest => dest.CustomFields, opt => opt.MapFrom(src => src.CustomFields));

        CreateMap<Item, ItemDTO>();
        CreateMap<CustomFieldValueDTO, CustomFieldValue>().ReverseMap();
        CreateMap<User, UserSearchDTO>();
        CreateMap<User, UserManagementDTO>();
        CreateMap<CommentDTO, Comment>();

        CreateMap<FieldDefinition, FieldStatsDTO>()
            .ForMember(dest => dest.FieldDefinitionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.MostPopularValues, opt => opt.Ignore());
    }
}
