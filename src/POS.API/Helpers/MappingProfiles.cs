using POS.Contract.Dtos.KitchenDtos;

namespace POS.API.Helpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CreateCompanyDto, Company>()
            .ForMember(c => c.NormalizedEnglishName,
            c =>
            c.MapFrom(c => c!.EnglishName!.ToUpper()))
           .ForMember(c => c.NormalizedEmail,
           c =>
           c.MapFrom(c => c!.Email!.ToUpper()));

        CreateMap<UpdatedCompanyDto, Company>()
           .ForMember(c => c.NormalizedEnglishName,
            c =>
            c.MapFrom(c => c!.EnglishName!.ToUpper()))
           .ForMember(c => c.NormalizedEmail,
           c =>
           c.MapFrom(c => c!.Email!.ToUpper()));


        CreateMap<BranchDto, Branch>()
           .ForMember(b => b.NormalizedName,
           b =>
           b.MapFrom(b => b!.Name!.ToUpper()))
           .ForMember(b => b.ImagePath, b => b.Ignore());

        CreateMap<UpdatedBranchDto, Branch>()
           .ForMember(b => b.NormalizedName,
           b =>
           b.MapFrom(b => b!.Name!.ToUpper()))
           .ForMember(b => b.ImagePath, b => b.Ignore());

        CreateMap<Branch, BranchToReturnDto>()
            .ForMember(b => b.Logo, b =>
            b!.MapFrom<ImageUrlResolver<Branch, BranchToReturnDto>>());

        CreateMap<CategoryDto, Category>()
            .ForMember(c => c.Id, c => c.Ignore())
            .ForMember(c => c.NormalizedEnglishName, c =>
            c.MapFrom(c => c!.EnglishName!.ToUpper()));

        CreateMap<UpdatedCategoryDto, Category>()
            .ForMember(c => c.NormalizedEnglishName, c => c.MapFrom(c => c!.EnglishName!.ToUpper()));


        CreateMap<CreateAttributeDto, Attributes>()
            .ForMember(a => a.Id, a => a.Ignore());

        CreateMap<AttributeItem, AttributeItemToReturnDto>();
        CreateMap<Category, CategoryToReturnDto>();

        CreateMap<AttributeItemDto, AttributeItem>();

        CreateMap<MenuSalesItemsDto, MenuSalesItems>()
            .ForMember(s => s.ImagePath, s => s.Ignore())
            .ForMember(s => s.NormalizedEnglishName, s =>
            s.MapFrom(s => s!.EnglishName!.ToUpper()))
            .ForMember(i => i.AttributeId, i =>
            i.MapFrom(i => i.AttributeId));





        CreateMap<UpdatedItemDto, MenuSalesItems>()
            .ForMember(s => s.NormalizedEnglishName, s =>
            s.MapFrom(s => s!.EnglishName!.ToUpper()));


        CreateMap<Attributes, AttributeToReturnDto>()
            .ForMember(dest => dest.AttributeItems, opt =>
                opt.MapFrom(src => src.AttributeItems));

        CreateMap<AttributeItem, AttributeItemToReturnDto>();


        CreateMap<UpdatedAttributeDto, Attributes>()
            .ForMember(dest => dest.AttributeItems, opt =>
                opt.MapFrom(src => src.AttributeItems));

        CreateMap<MenuSalesItems, MenuSalesItemsToReturnDto>()
    .ForMember(s => s.ImageUrl,
        s => s.MapFrom<ImageUrlResolver<MenuSalesItems, MenuSalesItemsToReturnDto>>())
    .ForMember(dest => dest.PrintInBackupReceiptFromItem,
    src =>
    src.MapFrom(s => s.PrintInBackupReceipt))
    .ForMember(dest => dest.PrintInBackupReceiptFromCategory,
    src =>
    src.MapFrom(s => s.Category!.PrintInBackupReceipt))
    .ForMember(dest => dest.CategoryId,
        c => c.MapFrom(c => c.CategoryId))
    .ForMember(dest => dest.ItemKitchenTypeId,
        src => src.MapFrom(i => i.KitchenTypeId))
    .ForMember(dest => dest.CategoryKitchenTypeId,
        src => src.MapFrom(i => i.Category != null ? i.Category.KitchenTypeId : null))
    .ForMember(dest => dest.Attributes,
        opt => opt.MapFrom(src =>
            src.HasAttribute && src.Attribute != null
                ? src.Attribute.AttributeItems.Select(ai => new MenuSalesItemAttributes
                {
                    AppearanceIndex = ai.AppearanceIndex,
                    GroupItems = new List<MenuSalesItemsGroupDto>
                    {
                        new MenuSalesItemsGroupDto
                        {
                            Id = ai.Id,
                            ArabicName = ai.RelatedMenuItem!.ArabicName,
                            EnglishName = ai.RelatedMenuItem.EnglishName,
                            Price = ai.RelatedMenuItem.Price,
                            AttributePrice = ai.RelatedMenuItem.AttributePrice
                        }
                    }
                }).ToList()
                : null
        ));


        CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.UserId, src => src.MapFrom(s => s.Id));

        CreateMap<TableDto, Table>();
        CreateMap<TableGroupDto, TableGroup>();
        CreateMap<TableGroupToReturnDto, TableGroup>().ReverseMap();
        CreateMap<Table, TableToReturnDto>()
            .ForMember(x => x.TableState, src => src.MapFrom(s => s.TableState.ToString()))
            .ReverseMap();


        CreateMap<AppUser, CaptainOrderUserToReturnDto>();

        CreateMap<AppDate, AppDateToReturnDto>();

        CreateMap<DeliveryZone, DeliveryZonesToReturnDto>().ReverseMap();
        CreateMap<DeliveryZoneDto, DeliveryZone>().ReverseMap();


        CreateMap<DeliveryCustomerInfo, DeliveryCustomerToReturnDto>()
        .ForMember(dest => dest.CustomerAddresses,
             opt =>
                opt.MapFrom(src => src.CustomerAddresses));

        CreateMap<CustomerAddress, CustomerAddressDto>();


        CreateMap<DeliveryCustomerDto, DeliveryCustomerInfo>()
            .ForMember(dest => dest.CustomerAddresses, opt => opt.MapFrom(src => new List<CustomerAddress>
            {
                new CustomerAddress
                {
                    BranchName = src.BranchName,
                    ZoneName = src.ZoneName,
                    HomeNumber = src.HomeNumber,
                    FloorNumber = src.FloorNumber,
                    FlatNumber = src.FlatNumber,
                    ClientAddress = src.ClientAddress,
                    AddressNote = src.AddressNote
                }
            }));

        CreateMap<DeliveryCustomerInfo, DeliveryCustomerDto>()
          .ForMember(dest => dest.ClientAddress,
          opt =>
          opt.MapFrom(src => src.CustomerAddresses));


        CreateMap<CustomerNewAddressDto, CustomerAddress>();


        CreateMap<KitchenType, KitchenTypeToReturnDto>()
               .ForMember(dest => dest.KitchenPrinter, opt
               => opt.MapFrom(src => src.KitchenPrinters))
               .ForMember(dest => dest.KitchenPrinterId, opt
               => opt.MapFrom(src => src.KitchenPrinterId));

        CreateMap<KitchenPrinters, KitchenPrintersDto>().ReverseMap();


        CreateMap<KitchenTypeDto, KitchenType>();
        CreateMap<KitchenPrinters, KitchenPrintersToReturnDto>();


        CreateMap<Orders, OrderDto>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderID))
            .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.BranchID))
            .ForMember(dest => dest.TakeAwayCustomerName, opt => opt.MapFrom(src => src.TakeawayCustomerName))
            .ForMember(dest => dest.TakeawayCustomerPhone, opt => opt.MapFrom(src => src.TakeawayCustomerPhone))
            .ForMember(dest => dest.TotalOrderDiscount, opt => opt.MapFrom(src => src.TotalDiscount))
            .ForMember(dest => dest.Remaining, opt => opt.MapFrom(src => src.Remain))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Service))
            .ForMember(dest => dest.OrderType, opt => opt.MapFrom(src => src.OrderType.ToString()))
            .ForMember(dest => dest.OrderState, opt => opt.MapFrom(src => src.OrderState.ToString()))
            .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails)) // هنعمل مابنج تاني للجوه
            .ForAllMembers(opts => opts.Ignore());

        CreateMap<OrderItemsDetails, TableItem>();
    }
}