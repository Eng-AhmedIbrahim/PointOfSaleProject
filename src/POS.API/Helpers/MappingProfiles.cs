namespace POS.API.Helpers;

public class MappingProfiles:Profile
{
    public MappingProfiles()
    {
        CreateMap<CreateCompanyDto, Company>()
            .ForMember(c => c.NormalizedEnglishName,
            c =>
            c.MapFrom(c => c.EnglishName.ToUpper()))
           .ForMember(c => c.NormalizedEmail,
           c =>
           c.MapFrom(c => c.Email.ToUpper()));

        CreateMap<UpdatedCompanyDto, Company>()
           .ForMember(c => c.NormalizedEnglishName,
            c =>
            c.MapFrom(c => c.EnglishName.ToUpper()))
           .ForMember(c => c.NormalizedEmail,
           c =>
           c.MapFrom(c => c.Email.ToUpper()));


        CreateMap<BranchDto, Branch>()
           .ForMember(b => b.NormalizedName,
           b =>
           b.MapFrom(b => b.Name.ToUpper()))
           .ForMember(b => b.ImagePath, b => b.Ignore());

        CreateMap<UpdatedBranchDto, Branch>()
           .ForMember(b => b.NormalizedName,
           b =>
           b.MapFrom(b => b.Name.ToUpper()))
           .ForMember(b => b.ImagePath, b => b.Ignore());

        CreateMap<Branch, BranchToReturnDto>()
            .ForMember(b => b.Logo, b =>
            b.MapFrom<ImageUrlResolver<Branch, BranchToReturnDto>>());

        CreateMap<CategoryDto, Category>()
            .ForMember(c => c.Id, c => c.Ignore())
            .ForMember(c => c.NormalizedEnglishName, c =>
            c.MapFrom(c => c.EnglishName.ToUpper()));

        CreateMap<UpdatedCategoryDto, Category>();

        CreateMap<AttributeDto, Attributes>()
            .ForMember(a => a.Id, a => a.Ignore());

        CreateMap<AttributeItem, AttributeItemToReturnDto>();
        CreateMap<Category, CategoryToReturnDto>();

        CreateMap<AttributeItemDto, AttributeItem>();

        CreateMap<MenuSalesItemsDto, MenuSalesItems>()
            .ForMember(s => s.ImagePath, s => s.Ignore())
            .ForMember(s => s.NormalizedEnglishName, s =>
            s.MapFrom(s => s.EnglishName.ToUpper()));

        CreateMap<MenuSalesItems, MenuSalesItemsToReturnDto>()
            .ForMember<string>(s => s.ImageUrl,
            s =>
            s.MapFrom<ImageUrlResolver<MenuSalesItems, MenuSalesItemsToReturnDto>>());
            

        CreateMap<UpdatedItemDto, MenuSalesItems>()
            .ForMember(s => s.NormalizedEnglishName, s =>
            s.MapFrom(s => s.EnglishName.ToUpper()));


        CreateMap<Attributes, AttributeToReturnDto>()
            .ForMember(dest => dest.AttributeItems, opt =>
                opt.MapFrom(src => src.AttributeItems));

        CreateMap<AttributeItem, AttributeItemToReturnDto>();


        CreateMap<UpdatedAttributeDto, Attributes>()
            .ForMember(dest => dest.AttributeItems, opt =>
                opt.MapFrom(src => src.AttributeItems));
    }
}